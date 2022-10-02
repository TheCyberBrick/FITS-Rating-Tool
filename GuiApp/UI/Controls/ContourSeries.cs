/*
    FITS Rating Tool
    Copyright (C) 2022 TheCyberBrick
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using Avalonia;
using OxyPlot;
using OxyPlot.Avalonia;
using System;

namespace FitsRatingTool.GuiApp.UI.Controls
{
    public class ContourSeries : XYAxisSeries
    {
        public static readonly StyledProperty<double[,]> DataProperty = AvaloniaProperty.Register<ContourSeries, double[,]>(nameof(Data), new double[1, 1], validate: v => v != null && v.Length > 0, coerce: (obj, v) => v == null || v.Length == 0 ? new double[1, 1] : v);

        public static readonly StyledProperty<double> ContourLevelStepProperty = AvaloniaProperty.Register<ContourSeries, double>(nameof(ContourLevelStep), 0.1, validate: v => v > 0);

        public static readonly StyledProperty<double[]> ContourLevelsProperty = AvaloniaProperty.Register<ContourSeries, double[]>(nameof(ContourLevels), new double[0], validate: v => v != null);

        public double[,] Data
        {
            get
            {
                return GetValue(DataProperty);
            }
            set
            {
                SetValue(DataProperty, value);
            }
        }

        public double ContourLevelStep
        {
            get
            {
                return GetValue(ContourLevelStepProperty);
            }
            set
            {
                SetValue(ContourLevelStepProperty, value);
            }
        }

        public double[] ContourLevels
        {
            get
            {
                return GetValue(ContourLevelsProperty);
            }
            set
            {
                SetValue(ContourLevelsProperty, value);
            }
        }

        static ContourSeries()
        {
            DataProperty.Changed.AddClassHandler<ContourSeries>(DataChanged);
            ContourLevelStepProperty.Changed.AddClassHandler<ContourSeries>(AppearanceChanged);
            ContourLevelsProperty.Changed.AddClassHandler<ContourSeries>(AppearanceChanged);
        }

        public ContourSeries()
        {
            InternalSeries = new OxyPlot.Series.ContourSeries { Data = Data };
        }

        public override OxyPlot.Series.Series CreateModel()
        {
            SynchronizeProperties(InternalSeries);
            return InternalSeries;
        }

        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.ContourSeries s)
            {
                s.Color = OxyColor.FromArgb(0, 0, 0, 0);
                s.TextColor = OxyColor.FromArgb(255, 0, 0, 0);
                s.LabelBackground = OxyColor.FromArgb(0, 0, 0, 0);
                s.Background = OxyColor.FromArgb(0, 0, 0, 0);
                s.LabelFormatString = "0.##";

                if (ContourLevels != null && ContourLevels.Length > 0)
                {
                    s.ContourLevels = ContourLevels;
                }
                else
                {
                    s.ContourLevelStep = ContourLevelStep;
                }

                s.Data = Data ?? new double[0, 0];

                s.ColumnCoordinates = ArrayBuilder.CreateVector(-1, 1, s.Data.GetLength(0));
                s.RowCoordinates = ArrayBuilder.CreateVector(-1, 1, s.Data.GetLength(1));

                if (s.Data.Length > 0)
                {
                    s.CalculateContours();
                }
            }
        }
    }
}
