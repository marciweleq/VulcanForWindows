using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LiteDB.Async;
using Newtonsoft.Json;
using Vulcanova.Core.Uonet;
using Vulcanova.Features.Auth;
using Vulcanova.Features.Auth.Accounts;
using Vulcanova.Features.Shared;
using Vulcanova.Uonet.Api;
using Vulcanova.Uonet.Api.Grades;
using VulcanTest.Vulcan;

namespace Vulcanova.Features.Grades;

public class GradesService : UonetResourceProvider
{
    public async Task<Grade[]> FetchGradesFromCurrentPeriodAsync(Account account)
        => await FetchPeriodGradesAsync(account, account.CurrentPeriod.Id);

    public async Task<Grade[]> FetchPeriodGradesAsync(Account account, int periodId)
    {
        var client = await new ApiClientFactory().GetAuthenticatedAsync(account);

        var normalGradesLastSync = GetLastSync(GetGradesResourceKey(account, periodId));

        var normalGradesQuery =
            new GetGradesByPupilQuery(account.Unit.Id, account.Pupil.Id, periodId, normalGradesLastSync, 500);

        var normalGrades = client.GetAllAsync(GetGradesByPupilQuery.ApiEndpoint,
            normalGradesQuery);

        var behaviourGradesLastSync = GetLastSync(GetGradesResourceKey(account, periodId));

        var behaviourGradesQuery =
            new GetBehaviourGradesByPupilQuery(account.Unit.Id, account.Pupil.Id, periodId, behaviourGradesLastSync,
                500);

        var behaviourGrades = client.GetAllAsync(GetBehaviourGradesByPupilQuery.ApiEndpoint,
            behaviourGradesQuery);
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<GradeMapperProfile>(); // Replace with your actual mapping profile class
        });

        IMapper mapper = mapperConfig.CreateMapper();


        var domainGrades = await normalGrades
            .Concat(behaviourGrades)
            .Select(mapper.Map<Grade>)
            .ToArrayAsync();

        foreach (var grade in domainGrades)
        {
            grade.AccountId = account.Id;
        }

        return domainGrades;
    }

    private static string GetGradesResourceKey(Account account, int periodId)
        => $"Grades_{account.Id}_{account.Pupil.Id}_{periodId}";

    private static string GetBehaviourGradesResourceKey(Account account, int periodId)
        => $"BehaviourGrades_{account.Id}_{account.Pupil.Id}_{periodId}";

    public override TimeSpan OfflineDataLifespan => TimeSpan.FromMinutes(15);
}

public static class GradesRepository
{
    private static LiteDatabaseAsync _db => LiteDbManager.database;

    public static IDictionary<string, IEnumerable<Grade>> buffer = new Dictionary<string, IEnumerable<Grade>>();

    public static async Task<IEnumerable<Grade>> GetGradesForPupilAsync(int accountId, int pupilId, int periodId)
    {
        string code = $"{accountId}.{pupilId}.{periodId}";

        if (buffer.TryGetValue(code, out var d))
        {
            return d;
        }

        var v = (await LiteDbManager.database.GetCollection<Grade>()
                .FindAsync(g => g.PupilId == pupilId && g.AccountId == accountId && g.Column.PeriodId == periodId))
            .OrderBy(g => g.Column.Subject.Name)
            .ThenBy(g => g.DateCreated);

        buffer[code] = v;

        return v;
    }

    public static async Task UpdatePupilGradesAsync(IEnumerable<Grade> newGrades)
    {
        await LiteDbManager.database.GetCollection<Grade>().UpsertAsync(newGrades);
    }
}