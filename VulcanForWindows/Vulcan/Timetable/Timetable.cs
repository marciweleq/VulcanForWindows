﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulcanova.Features.Auth.Accounts;
using Vulcanova.Features.Timetable;
using Vulcanova.Features.Timetable.Changes;
using VulcanTest.Vulcan.Timetable.Changes;

namespace VulcanTest.Vulcan.Timetable
{
    public static class Timetable
    {

        public static async Task<IReadOnlyDictionary<DateTime, IReadOnlyCollection<TimetableListEntry>>> FetchEntriesForRange(Account account, DateTime from, DateTime to)
        {
            from = from.Date;
            to = to.Date;
            var og = (await new OgTimetable().FetchEntriesForRange(account, from, to));

            IEnumerable<TimetableChangeEntry> c = (await new TimetableChanges().FetchEntriesForRange(account, from, to));

            IReadOnlyDictionary<DateTime, IReadOnlyCollection<TimetableListEntry>> l = (TimetableBuilder.BuildTimetable(og, c as ICollection<TimetableChangeEntry>));
            return l;
        }

        public static async Task<IReadOnlyCollection<TimetableListEntry>> FetchEntriesForDay(Account account, DateTime day) =>
        (await FetchEntriesForRange(account, new DateTime(day.Year, day.Month, day.Day),
            new DateTime(day.Year, day.Month, day.Day, 23, 59, 59))).Values.ElementAt(0);
        public static async Task<IReadOnlyDictionary<DateTime, IReadOnlyCollection<TimetableListEntry>>> FetchEntriesForMonthAndYear(Account account, DateTime monthAndYear) =>
            await FetchEntriesForRange(account, new DateTime(monthAndYear.Year, monthAndYear.Month, 1),
                new DateTime(monthAndYear.Year, monthAndYear.Month, DateTime.DaysInMonth(monthAndYear.Year, monthAndYear.Month), 23, 59, 59));
    }
}
