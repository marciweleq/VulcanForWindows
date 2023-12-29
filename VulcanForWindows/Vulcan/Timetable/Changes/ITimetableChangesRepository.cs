using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VulcanTest.Vulcan.Timetable.Changes;

namespace Vulcanova.Features.Timetable.Changes;

public interface ITimetableChangesRepository
{
    Task<IEnumerable<TimetableChangeEntry>> GetEntriesForPupilAsync(int accountId, int pupilId,
        DateTime monthAndYear);

    Task UpsertEntriesAsync(IEnumerable<TimetableChangeEntry> entries, DateTime monthAndYear);
}