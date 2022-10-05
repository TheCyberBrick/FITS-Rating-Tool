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

using ReactiveUI;
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.ImageAnalysis
{
    public interface IImageAnalysisViewModel
    {
        public interface IFactory
        {
            IImageAnalysisViewModel Create(string file);
        }

        string File { get; }

        string FileName { get; }

        int DataKeyIndex { get; set; }

        int MaxSamples { get; set; }

        float Smoothness { get; set; }

        bool WeightedSmoothing { get; set; }

        bool NormalizeDimensions { get; set; }

        int Steps { get; set; }

        bool HasSteps { get; }

        int HorizontalResolution { get; set; }

        int VerticalResolution { get; set; }

        double[,] RawData { get; }

        double[,] Data { get; }

        double DataStepSize { get; }

        double[] DataSteps { get; }

        int DataSamples { get; }

        ReactiveCommand<Unit, Unit> Reset { get; }
    }
}
