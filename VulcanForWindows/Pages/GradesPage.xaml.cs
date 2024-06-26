using DevExpress.WinUI.Core.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using VulcanForWindows.Classes;
using VulcanForWindows.Classes.Grades;
using VulcanForWindows.Extensions;
using VulcanForWindows.Vulcan;
using Vulcanova.Features.Auth;
using Vulcanova.Features.Grades;
using Vulcanova.Features.Shared;
using VulcanTest.Vulcan;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VulcanForWindows.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GradesPage : Page, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<Period> AllPeriods { get; set; }
        public string[] DisplayPeriods { get => AllPeriods.Select(r => $"Klasa {r.Level}, Semestr {r.Number}").ToArray(); }

        public static IDictionary<int, NewResponseEnvelope<Grade>> PeriodEnvelopes = new Dictionary<int, NewResponseEnvelope<Grade>>();

        public Period SelectedPeriod
            => AllPeriods[yearSelector.SelectedIndex];

        public ObservableCollection<SubjectGrades> SubjectGrades { get; set; } = new ObservableCollection<SubjectGrades>();
        public ObservableCollection<Grade> RecentGrades { get; set; } = new ObservableCollection<Grade>();

        public GradesPage()
        {
            this.InitializeComponent();
            AllPeriods = new AccountRepository().GetActiveAccount().Periods
                .Select(r => r).ToList();
            OnPropertyChanged(nameof(DisplayPeriods));
            yearSelector.SelectedIndex = AllPeriods.FindIndex(r => r.Id == new AccountRepository().GetActiveAccount().CurrentPeriod.Id);
        }

        private void SelectedYearChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is FlipView flipView)
            {
                //SubjectGrades.ReplaceAll(new SubjectGrades[0]);
                Up();
            }
        }

        async void Up() => LoadSubjectGrades();

        async void LoadSubjectGrades()
        {
            await Task.Delay(100);
            ProgressBar.Visibility = Visibility.Visible;
            if (PeriodEnvelopes.TryGetValue(SelectedPeriod.Id, out var v))
            {
                await v.Sync();
                GradesLoaded(v.Entries.ToArray());

            }
            else
            {
                PeriodEnvelopes[SelectedPeriod.Id] = await new GradesService().GetPeriodGradesV3(new AccountRepository().GetActiveAccount(), SelectedPeriod.Id, waitForSync: true, forceSync: false);
                IEnumerable<Grade> d = PeriodEnvelopes[SelectedPeriod.Id].Entries.ToArray();
                GradesLoaded(d);
            }
            ProgressBar.Visibility = Visibility.Collapsed;
        }

        public void GradesLoaded(IEnumerable<Grade> grades)
        {
            Debug.WriteLine(String.Join(",", grades.Select(r => r.Column.Color)));
            var generated = GradesHelper.GenerateSubjectGrades(grades);
            SubjectGrades = new ObservableCollection<SubjectGrades>(generated);
            SubjectGradesList.ItemsSource = SubjectGrades;
            RecentGrades.ReplaceAll(grades.Where(r => (r.HasBeenModifiedByTeacher ? r.DateModify : r.DateCreated) > DateTime.Now.AddDays(-14))
                .OrderByDescending(r => (r.HasBeenModifiedByTeacher ? r.DateModify : r.DateCreated)));
        }

    }
}
