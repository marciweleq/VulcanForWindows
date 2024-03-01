﻿using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.VisualElements;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Vulcanova.Features.Grades;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VulcanForWindows.UserControls.ClassmatesGrades
{
    public sealed partial class SingleClassmateGrades : UserControl, INotifyPropertyChanged
    {


        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static readonly DependencyProperty UserGradeProperty =
            DependencyProperty.Register("UserGrade", typeof(Grade), typeof(SingleClassmateGrades), new PropertyMetadata(null, Grade_Changed));

        public Grade UserGrade
        {
            get => (Grade)GetValue(UserGradeProperty);
            set => SetValue(UserGradeProperty, value);
        }


        public static readonly DependencyProperty ColumnIdProperty =
            DependencyProperty.Register("ColumnId", typeof(int), typeof(SingleClassmateGrades), new PropertyMetadata(null, ColumnId_Changed));

        private static void ColumnId_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SingleClassmateGrades control && e.NewValue is int newValue)
            {

                //control.GenerateChart();
                //Debug.WriteLine("CID:" + newValue);

            }
        }
        private static void Grade_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SingleClassmateGrades control && e.NewValue is Grade newValue)
            {
                control.GenerateChart(newValue.Column.Id, (double)newValue.Value.Value);
                Debug.WriteLine("CID2:" + newValue.Column.Id);

            }
        }

        public int ColumnId
        {
            get => (int)GetValue(ColumnIdProperty);
            set => SetValue(ColumnIdProperty, value);
        }
        public SingleClassmateGrades()
        {
            this.InitializeComponent();
            //GenerateChart();
        }
        double highestGrade = 0;
        float betterThanPercentile = 0;
        public bool DisplayLoadingIndicator { get; set; } = true;
        string betterThanDisplay => $"Lepiej niż {betterThanPercentile.ToString("0.00")}% klasy";
        string highestGradeDisplay => $"Najwyższa ocena w klasie to {highestGrade}";

        public bool ShouldDisplayChart => GradesAvaible >= MinGradesAvaibleToShowChart;

        int GradesAvaible { get; set; }
        int MinGradesAvaibleToShowChart { get; set; } = 4;
        public async void GenerateChart(int columnId, double userGrade = 0)
        {
            await Task.Delay(10);
            if (ColumnId == 0) return;
            var classmatesGrades = await Classes.VulcanGradesDb.ClassmateGradesService.GetSingleClassmateColumn(columnId);
            if (classmatesGrades == null) return;

            foreach (var v in classmatesGrades.Grades)
                if (v.Value > highestGrade) highestGrade = v.Value;

            GradesAvaible = classmatesGrades.Grades.Length;
            var groupped = classmatesGrades.Grades
    .GroupBy(r => Math.Round(r.Value - 0.01))
    .ToDictionary(
        r => r.Key,
        r => r.ToArray()
    );
            var list = new List<int>();

            double maxgrade = 6;

            int betterGradesCount = 0;
            int worseOrEqalGradesCount = 0;

            foreach (var grade in groupped)
            {
                if (grade.Key > maxgrade) maxgrade = Math.Ceiling(grade.Key);

                if (grade.Key > userGrade)
                    betterGradesCount += grade.Value.Length;
                if (grade.Key <= userGrade) worseOrEqalGradesCount += grade.Value.Length;
            }

            betterThanPercentile = (float)worseOrEqalGradesCount / (float)(betterGradesCount + worseOrEqalGradesCount) * 100f;

            List<float> avaibleGrades = new List<float>();
            for (int i = 1; i <= maxgrade; i++) avaibleGrades.Add(i);


            for (int i = 1; i <= maxgrade; i++)
            {
                if (groupped.TryGetValue(i, out var value))
                    list.Add(value.Length);
                else
                    list.Add(0);
            }
            var lArray = list.ToArray();
            var labels = avaibleGrades.Select(r => r.ToString()).Where(r => !string.IsNullOrEmpty(r)).ToArray();
            Series = new ISeries[]
            {
                new LineSeries<int>
                {
                Values = lArray,
                XToolTipLabelFormatter = (chartPoint) =>
                    {
                        var tooltipContent = $"Ocena {labels[chartPoint.Index]}";
                        return tooltipContent;
                    },
                }
            };

            XAxes = new List<Axis>
            {
                new Axis()
                {
                    Labels =labels,

                }

            };
            
            grid.DataContext = this;
            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(betterThanDisplay));
            OnPropertyChanged(nameof(highestGradeDisplay));
            OnPropertyChanged(nameof(GradesAvaible));
            OnPropertyChanged(nameof(ShouldDisplayChart));
            DisplayLoadingIndicator = false;
            OnPropertyChanged(nameof(DisplayLoadingIndicator));

        }
        public ISeries[] Series { get; set; } = new ISeries[0];

        public List<Axis> XAxes { get; set; } = new List<Axis>();

        private void FontIcon_PointerEntered2(object sender, PointerRoutedEventArgs e)
        {
            TeachingTipClassmateGrades2.IsOpen = true;
        }

        private void FontIcon_PointerExited2(object sender, PointerRoutedEventArgs e)
        {
            TeachingTipClassmateGrades2.IsOpen = false;

        }
    }
}
