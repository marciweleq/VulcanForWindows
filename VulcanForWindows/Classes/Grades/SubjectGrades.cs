﻿using DevExpress.Pdf;
using DevExpress.Pdf.Native.BouncyCastle.Asn1.X509;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VulcanForWindows.Classes.Grades;
using VulcanForWindows.Vulcan.Grades;
using VulcanForWindows.Vulcan.Grades.Final;
using Vulcanova.Features.Auth;
using Vulcanova.Features.Grades;
using Vulcanova.Features.Grades.Final;
using Vulcanova.Features.Shared;

namespace VulcanForWindows.Classes
{
    public class SubjectGrades : INotifyPropertyChanged
    {
        public Subject subject { get; set; }

        public int periodId => (grades.Count == 0) ? 0 : (grades.FirstOrDefault().Column.PeriodId);

        public ObservableCollection<SubjectGradesGrade> grades
        {
            get; set;
        }

        public List<SubjectGradesGrade> addedGrades = new List<SubjectGradesGrade>();
        public List<SubjectGradesGrade> excludedGrades = new List<SubjectGradesGrade>();

        public SubjectGrades() { }
        public SubjectGrades(Subject subject, Grade[] g, string fGrade = "", int trim = 0, bool loadFinalGrade = true)
        {
            this.subject = subject;
            grades = new ObservableCollection<SubjectGradesGrade>(SubjectGradesGrade.Get(g.Where(r => r.Column.Subject.Id == subject.Id)));
            finalGrade = fGrade;

            FetchYearlyAverage();

            if (trim > 0) grades = new ObservableCollection<SubjectGradesGrade>(grades.ToArray().Take(trim).ToArray());

            //class average for all grades
            foreach (var v in grades) v.CalculateClassAverage();

            //term average
            CalculateTermAverage();

            if (loadFinalGrade)
                GetFinalGrade();
        }

        private void CalculateTermAverage(bool calcHipothetical = false)
        {
            var calculateFrom = grades.Where(r => r.ActualValue.HasValue).Where(r => r.Column.Weight != 0);
            if (!calcHipothetical) calculateFrom = calculateFrom.Where(r => !r.IsHipothetic).ToList();
            else calculateFrom = calculateFrom.Where(r => !excludedGrades.Where(r => !r.IsHipothetic).Select(p => p.Id).ToList().Contains(r.Id)).ToList();

            if (calculateFrom.Count() > 0)
            {
                var average = (double)calculateFrom.Where(r => r.ActualValue.HasValue).Where(r => r.Column.Weight != 0).Select(r => Enumerable.Repeat(r.ActualValue.Value, r.Column.Weight)
                .ToList()).ToList().SelectMany(list => list).ToList().Average();

                if (calcHipothetical)
                {
                    termModifiedAverage = average;
                    termModifiedGradesCount = calculateFrom.Count();
                    OnPropertyChanged(nameof(termModifiedAverage));
                    OnPropertyChanged(nameof(termModifiedGradesCount));
                }
                else
                {
                    termActualAverage = average;
                    termActualGradesCount = calculateFrom.Count();
                    OnPropertyChanged(nameof(termActualAverage));
                    OnPropertyChanged(nameof(termActualGradesCount));
                }


                OnPropertyChanged(nameof(termDisplayedAverage));
                OnPropertyChanged(nameof(termDisplayedGradesCount));
            }
        }

        public async Task<string> GetFinalGrade()
        {
            var finalGrades =
                await new FinalGrades().GetPeriodGrades(new AccountRepository().GetActiveAccount(),
                periodId, false, true);
            var g = finalGrades.Grades.Where(r => r.Subject.Id == subject.Id);
            if (g.Count() > 0)
            {
                var f = g.First();
                if (string.IsNullOrEmpty(f.FinalGrade))
                {
                    if (FinalGradeParser.TryGetValueFromDescriptiveForm(f.PredictedGrade, out var v))
                        finalGrade = v.ToString("0");
                }
                else
                    if (FinalGradeParser.TryGetValueFromDescriptiveForm(f.FinalGrade, out var v))
                    finalGrade = v.ToString("0");

                isFinalGradePredicted = string.IsNullOrEmpty(f.FinalGrade);

                //if (string.IsNullOrEmpty(finalGrade)) finalGrade = g.First().PredictedGrade;
                OnPropertyChanged(nameof(finalGrade));
                OnPropertyChanged(nameof(hasFinalGrade));
                OnPropertyChanged(nameof(isFinalGradePredicted));
            }
            return finalGrade;
        }




        public Visibility removeButtonVisibility => (addedGrades.Count == 0) ? Visibility.Collapsed : Visibility.Visible;

        public void BringBackToReal()
        {
            removeAddedGrades();
            includeExcludedGrades();
        }

        public void excludeGrade(SubjectGradesGrade g)
        {
            g.isBeingExcluded = true;
            excludedGrades.Add(g);

            AddedOrRemovedGrades();
        }
        public void includeGradeBack(SubjectGradesGrade g)
        {
            g.isBeingExcluded = false;
            excludedGrades.Remove(g);

            AddedOrRemovedGrades();
        }
        public void includeExcludedGrades()
        {
            foreach (var v in excludedGrades)
            {
                v.isBeingExcluded = false;
            }

            excludedGrades.Clear();

            AddedOrRemovedGrades();
        }
        public void removeAddedGrades()
        {
            foreach (var v in addedGrades)
                grades.Remove(v);

            addedGrades.Clear();

            AddedOrRemovedGrades();
        }
        public void AddGrade(Grade grade)
        {
            var c = SubjectGradesGrade.Get(grade);
            grades.Add(c);
            addedGrades.Add(c);

            AddedOrRemovedGrades();
        }
        public void removeAddedGrade(SubjectGradesGrade g)
        {
            grades.Remove(g);

            addedGrades.Remove(g);

            AddedOrRemovedGrades();
        }

        public void AddedOrRemovedGrades()
        {
            CalculateTermAverage(true);
            GetYearlyAverage(true, true);

            OnPropertyChanged(nameof(isDisplayingActualAverages));
            OnPropertyChanged(nameof(termDisplayedAverage));
            OnPropertyChanged(nameof(termDisplayedGradesCount));
            OnPropertyChanged(nameof(yearDisplayedAverage));
            OnPropertyChanged(nameof(yearDisplayedGradesCount));

        }

        public Grade[] recentGrades => grades.OrderBy(r => r.DateCreated).ToList().Take(10).ToArray();
        public string finalGrade { get; set; }
        public bool hasFinalGrade { get => !string.IsNullOrEmpty(finalGrade); }
        public bool isFinalGradePredicted { get; set; }

        [JsonIgnore]
        public GradesCountChartData gradesCountChart => GradesCountChartData.Generate(grades.ToArray());

        IDictionary<Period, Grade[]> _yearGrades;
        public static IDictionary<string, ((double average, int count, int weightSum) data, DateTime generatedAt)> YearlyAverages = new Dictionary<string, ((double average, int count, int weightSum) data, DateTime generatedAt)>();
        public async void FetchYearlyAverage() => await GetYearlyAverage();
        public async Task<(double average, int count, int weightSum)> GetYearlyAverage(bool force = false, bool includeAddedGrades = false)
        {
            var v = await CalculateYearlyAverage(null, force, includeAddedGrades);

            if (!includeAddedGrades)
            {
                yearActualAverage = v.average;
                yearActualGradesCount = v.count;

                OnPropertyChanged(nameof(yearActualAverage));
                OnPropertyChanged(nameof(yearActualGradesCount));

            }
            else
            {
                yearModifiedAverage = v.average;
                yearModifiedGradesCount = v.count;

                OnPropertyChanged(nameof(yearModifiedAverage));
                OnPropertyChanged(nameof(yearModifiedGradesCount));
            }

            return v;
        }

        private string GetYearlyAverageId(IEnumerable<Grade> addedGrades = null)
            => $"{$"{((periodId % 2 == 0) ? periodId : (periodId - 1))}_{subject.Id}"}{(addedGrades == null ? "" : ("__withAddedGrades_" + string.Join(',', addedGrades.Select(r => r.Id))))}";


        public async Task<(double average, int count, int weightSum)> CalculateYearlyAverage(Grade[] excludeGrades = null, bool forceSlowMethod = false, bool calcHipothetical = false)
        {
            //if (excludeGrades == null) excludeGrades = new Grade[0];
            //var YearlyAveragesEntryKey = GetYearlyAverageId(includeAddedGrades ? addedGrades : null) +
            //    string.Join(',', excludeGrades.Select(r => r.Id).ToArray());


            //if (YearlyAverages.TryGetValue(YearlyAveragesEntryKey, out var o))
            //{
            //    if (DateTime.Now - o.generatedAt < new TimeSpan(0, 15, 0))
            //        return o.data;
            //    else
            //        YearlyAverages.Remove(YearlyAveragesEntryKey);
            //}

            //if (!forceSlowMethod && (YearlyAverages.TryGetValue(GetYearlyAverageId(includeAddedGrades ? addedGrades : null), out var YearlyAveragesData)))
            //{
            //    var sum = YearlyAveragesData.data.average * (double)YearlyAveragesData.data.weightSum;
            //    excludeGrades = excludeGrades.Where(r => r.ActualValue.HasValue && r.Column.Weight != 0).ToArray();
            //    foreach (var excludedGrade in excludeGrades)
            //        sum -= (double)excludedGrade.ActualValue.Value * (double)excludedGrade.Column.Weight;
            //    var weightSum = YearlyAveragesData.data.weightSum - excludeGrades.Select(r => r.Column.Weight).Sum();

            //    var output = (sum / weightSum, YearlyAveragesData.data.count - excludeGrades.Length, weightSum);

            //    YearlyAverages.Add(YearlyAveragesEntryKey, (output, DateTime.Now));

            //    return output;
            //}
            //else
            //{
            //    var excludeIds = excludeGrades.Select(r => r.Id).ToList();
            //    if (_yearGrades == null) _yearGrades =
            //        (await (new GradesService()).FetchGradesFromLevelAsync(new AccountRepository().GetActiveAccount(), periodId));

            //    var gradesOnly = _yearGrades.SelectMany(r => r.Value).Where(r => r.Column.Subject.Id == subject.Id);
            //    var yearlyGrades = gradesOnly;
            //    if (includeAddedGrades) yearlyGrades = yearlyGrades.Concat(addedGrades).ToArray();

            //    var yearlyAverage = yearlyGrades.Where(r => ((excludeGrades == null) ? true : (!excludeIds.Contains(r.Id)))).ToArray().CalculateAverage();

            //    var output = (yearlyAverage, yearlyGrades.Count(), yearlyGrades.Select(r => r.Column.Weight).Sum());

            //    YearlyAverages[YearlyAveragesEntryKey] = (output, DateTime.Now);

            //    return output;
            //}
            if (excludeGrades == null)
                excludeGrades = new Grade[0];

            if (calcHipothetical)
                excludeGrades = excludeGrades.Concat(excludedGrades).ToArray();

            _yearGrades =
                    (await (new GradesService()).FetchGradesFromLevelAsync(new AccountRepository().GetActiveAccount(), periodId));
            var calculateFrom = _yearGrades.SelectMany(r => r.Value).Concat(addedGrades).ToList();
            if (excludeGrades.Length > 0)
                calculateFrom = calculateFrom.Where(r => !excludeGrades.Where(r => !r.IsHipothetic).Select(p => p.Id).ToList().Contains(r.Id)).ToList();
            calculateFrom = calculateFrom.Where(r => r.Column.Subject.Id == subject.Id).Where(r => r.ActualValue.HasValue).Where(r => r.Column.Weight != 0).ToList();
            if (!calcHipothetical) calculateFrom = calculateFrom.Where(r => !r.IsHipothetic).ToList();

            if (calculateFrom.Count() > 0)
            {
                var average = (double)calculateFrom.Where(r => r.ActualValue.HasValue).Where(r => r.Column.Weight != 0).Select(r => Enumerable.Repeat(r.ActualValue.Value, r.Column.Weight)
                .ToList()).ToList().SelectMany(list => list).ToList().Average();
                return (average, calculateFrom.Count(), calculateFrom.Select(r => r.Column.Weight).Sum());
            }

            return (0, 0, 0);
        }

        public double termActualAverage { get; set; }
        public int termActualGradesCount { get; set; }
        public double yearActualAverage { get; set; }
        public int yearActualGradesCount { get; set; }


        public double termModifiedAverage { get; set; }
        public int termModifiedGradesCount { get; set; }
        public double yearModifiedAverage { get; set; }
        public int yearModifiedGradesCount { get; set; }

        public bool isDisplayingActualAverages { get => addedGrades.Count == 0 && excludedGrades.Count == 0; }
        public double termDisplayedAverage { get => isDisplayingActualAverages ? termActualAverage : termModifiedAverage; }
        public int termDisplayedGradesCount { get => isDisplayingActualAverages ? termActualGradesCount : termModifiedGradesCount; }
        public double yearDisplayedAverage { get => isDisplayingActualAverages ? yearActualAverage : yearModifiedAverage; }
        public int yearDisplayedGradesCount { get => isDisplayingActualAverages ? yearActualGradesCount : yearModifiedGradesCount; }

        public static SubjectGrades[] CreateRecent(GradesResponseEnvelope e) => CreateRecent(e.Grades);
        public static SubjectGrades[] CreateRecent(IEnumerable<Grade> e)
        {
            int cutoutDateLimit = 40;
            int subjectsLimit = 5;
            int gradesPerSubjectLimit = 5;

            List<Grade> gradesDates = e.GroupBy(r => r.Column.Subject.Name).Select(r => r.OrderByDescending(d => d.DateModify).Take(5)).OrderByDescending(r => r.FirstOrDefault().DateCreated).SelectMany(r => r).ToList();

            var oldestPossible = (gradesDates[(gradesDates.Count > cutoutDateLimit) ? cutoutDateLimit : (gradesDates.Count - 1)]).DateModify;

            Grade[] grades = e.Where(r => r.DateCreated.GetValueOrDefault() >= oldestPossible).GroupBy(r => r.Column.Subject.Id)
                .OrderByDescending(r => r.ToArray().Last().DateModify).Take(subjectsLimit)
                .Select(r => r.ToArray().Take(gradesPerSubjectLimit)).SelectMany(list => list)
                .ToArray();

            return grades.GenerateSubjectGrades(false).ToArray();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
