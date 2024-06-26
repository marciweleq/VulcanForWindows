﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using VulcanForWindows.Classes;
using Windows.Foundation;
using Newtonsoft.Json;
using Windows.Foundation.Collections;
using System.Collections.ObjectModel;
using VulcanTest.Vulcan.Timetable;
using VulcanForWindows.Vulcan.Timetable;
using Vulcanova.Features.Auth;
using VulcanTest.Vulcan;
using System.Diagnostics;
using Vulcanova.Features.Timetable;
using System.Globalization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VulcanForWindows
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TimetablePage : Page
    {

        public TimetableResponseEnvelope env;
        TimetableDayGrouper tdg;
        IDictionary<DateTime, TimetableDayGrouper> tdgD = new Dictionary<DateTime, TimetableDayGrouper>();

        public TimetableDay[] display
        {
            get => (tdg == null) ? new TimetableDay[0] : (tdg.GetWeek(week));
        }

        public DateTime week
        {
            get => _week;
            set
            {
                _week = GetStartOfTheWeek(value);
                ChangeWeek();
            }
        }

        DateTime _week;

        public TimetablePage()
        {
            this.InitializeComponent();
            week = GetStartOfTheWeek(DateTime.Now);
            //appointments = new ObservableCollection<TimetableEntry>(TimetableEntry.Generate(RandomGenerator.GenerateRandomTimetable()));

        }

        public DateTime GetStartOfTheWeek(DateTime t) => TimetableDayGrouper.GetStartOfWeek(t, false);

        public bool isLoading;

        public async void ChangeWeek()
        {
            var acc = new AccountRepository().GetActiveAccount();

            //env = await new OgTimetable().GetPeriodEntriesByMonth(acc, DateTime.Today, false, false);
            //env.EntriesUpdated += HandleEntriesUpdated;
            //tdg = new TimetableDayGrouper(env.Entries);
            if (!tdgD.ContainsKey(GetStartOfTheWeek(week)))
            {
                isLoading = true;
                UpdateLoadingBarVisibility();
                tdgD.Add(GetStartOfTheWeek(week), new TimetableDayGrouper((await Timetable.FetchEntriesForRange(acc, week.AddDays(-7), week.AddDays(7))).SelectMany(r => r.Value)));
                isLoading = false;
            }
            tdg = tdgD[GetStartOfTheWeek(week)];

            Debug.Write("\nTDG loaded");
            Update();

            if (!tdgD.ContainsKey(GetStartOfTheWeek(week.AddDays(7))))
            {
                isLoading = true;
                var v = new TimetableDayGrouper((await Timetable.FetchEntriesForRange(acc, week.AddDays(7), week.AddDays(14))).SelectMany(r => r.Value));
                if (!tdgD.ContainsKey(GetStartOfTheWeek(week.AddDays(7))))
                    tdgD.Add(GetStartOfTheWeek(week.AddDays(7)), v);
                isLoading = false;
            }
            if (!tdgD.ContainsKey(GetStartOfTheWeek(week.AddDays(-7))))
            {
                isLoading = true;
                var v = new TimetableDayGrouper((await Timetable.FetchEntriesForRange(acc, week.AddDays(-14), week.AddDays(7))).SelectMany(r => r.Value));
                if (!tdgD.ContainsKey(GetStartOfTheWeek(week.AddDays(-14))))
                    tdgD.Add(GetStartOfTheWeek(week.AddDays(-14)), v);
                isLoading = false;
            }

            UpdateLoadingBarVisibility();

        }

        void UpdateLoadingBarVisibility()
        {
            LoadingBar.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        }

        //void HandleEntriesUpdated(object sender, IEnumerable<TimetableEntry> updatedGrades)
        //{
        //    tdg = new TimetableDayGrouper(env.Entries);
        //    lv.ItemsSource = display;
        //    lv.UpdateLayout();
        //}

        private void Update()
        {
            sel.Text = GetStartOfTheWeek(week).ToString("dd/MM") + " - " + GetStartOfTheWeek(week).AddDays(6).ToString("dd/MM");
            //lv.ItemsSource = display;
            //lv.UpdateLayout();

            gr.Children.Clear();

            for (int i = 0; i < display.Length; i++)
            {
                var sp = (this.Resources["DayTemp"] as DataTemplate).LoadContent() as UserControls.TimetableDayDisplayer;

                gr.Children.Add(sp);

                sp.Value = display[i];
                Grid.SetColumn(sp, i);
            }
            if (display.Length == 0)
            {
                var r = new TextBlock();
                r.Text = "Brak wpisów na ten okres.";
                r.TextAlignment = TextAlignment.Center;
                r.HorizontalAlignment = HorizontalAlignment.Center;
                r.VerticalAlignment = VerticalAlignment.Center;
                gr.Children.Add(r);
            }
        }

        private void Next(object sender, RoutedEventArgs e)
        {
            week = week.AddDays(7);
        }
        private void Prev(object sender, RoutedEventArgs e)
        {
            week = week.AddDays(-7);
        }

        private void ViewDetails(object sender, TappedRoutedEventArgs e)
        {
            //TODO: WYŚWIETL SZCZEGÓŁY LEKCJI
        }

        private void CurrentWeek(object sender, RoutedEventArgs e)
        {
            week = GetStartOfTheWeek(DateTime.Now);
        }
    }

    public class TimetableDayGrouper
    {
        public TimetableDayGrouper(IEnumerable<TimetableListEntry> e)
        {
            var v = new Dictionary<DateTime, List<TimetableListEntry>>();
            foreach (var entry in e)
            {
                if (!v.ContainsKey(entry.Date))
                    v.Add(entry.Date, new List<TimetableListEntry>());

                v[entry.Date].Add(entry);
            }
            Debug.Write(JsonConvert.SerializeObject(v) + "\n\n\np\n" + JsonConvert.SerializeObject(e));
            entries = v.Select(r => new KeyValuePair<DateTime, TimetableListEntry[]>(r.Key, r.Value.ToArray().OrderBy(d => d.Start.Value).ToArray())).OrderBy(r => r.Key).ToArray();
        }

        public KeyValuePair<DateTime, TimetableListEntry[]>[] entries { get; set; }

        public TimetableDay[] GetWeek(DateTime date)
        {
            date.AddDays(1);
            DateTime startOfWeek = GetStartOfWeek(date, false);
            DateTime endOfWeek = startOfWeek.AddDays(4); // Friday is 4 days after Monday

            var v= entries.Where(r => r.Key.Date >= startOfWeek.Date && r.Key.Date <= endOfWeek.Date).Select(r => new TimetableDay(r)).ToList();

            for(DateTime i = startOfWeek; i <= endOfWeek; i=i.AddDays(1))
            {
                if (v.Where(r => r.Date.Date == i).Count() == 0)
                    v.Add(new TimetableDay(new KeyValuePair<DateTime, TimetableListEntry[]>(i, new TimetableListEntry[0])));
            }

            v = v.OrderBy(r => r.Date).ToList();

            return v.ToArray();
        }

        public static DateTime GetStartOfWeek(DateTime date, bool weekBack = false)
        {
            var d = date.AddDays(-((int)date.DayOfWeek + -1));
            return d.Date;
        }
    }

    public class TimetableDay
    {

        public TimetableDay(KeyValuePair<DateTime, TimetableListEntry[]> e)
        {
            entries = e;
        }

        public KeyValuePair<DateTime, TimetableListEntry[]> entries { get; set; }

        public TimetableListEntry[] e => entries.Value;

        public int dayOfWeek => (int)entries.Key.DayOfWeek;
        public DateTime Date => entries.Key.Date;

        public string day => new CultureInfo("en-US", false).TextInfo.ToTitleCase(entries.Key.ToString("dddd, dd/MM"));
    }
}
