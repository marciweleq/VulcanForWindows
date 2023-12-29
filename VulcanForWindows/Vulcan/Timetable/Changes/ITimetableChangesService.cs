using System;
using System.Collections.Generic;
using VulcanTest.Vulcan.Timetable.Changes;

namespace Vulcanova.Features.Timetable.Changes;

public interface ITimetableChangesService
{
    IObservable<IEnumerable<TimetableChangeEntry>> GetChangesEntriesByMonth(int accountId, DateTime monthAndYear,
        bool forceSync = false);
}