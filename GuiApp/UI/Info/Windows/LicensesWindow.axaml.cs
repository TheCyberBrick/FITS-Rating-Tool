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

using Avalonia.Controls;
using Avalonia;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.UI.Info.Windows
{
    public partial class LicensesWindow : Window
    {
        private class License
        {
            public string Name { get; }

            public string Text { get; }

            public License(string name, string text)
            {
                Name = name;
                Text = text;
            }
        }

        public LicensesWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            var licenses = new List<License>
            {
                new License("FITS Rating Tool", Properties.Resources.LICENSE_FITSRatingTool),
                new License("PCL", Properties.Resources.LICENSE_PCL),
                new License("CFITSIO", Properties.Resources.LICENSE_CFITSIO),
                new License("SEP", Properties.Resources.LICENSE_SEP),
                new License("CSharpFITS", Properties.Resources.LICENSE_CSharpFITS),
                new License("CMinpack", Properties.Resources.LICENSE_CMinpack),
                new License("PThreads", Properties.Resources.LICENSE_PThreads),
                new License("MathEval", Properties.Resources.LICENSE_MathEval),
                new License("Avalonia", Properties.Resources.LICENSE_Avalonia),
                new License("PanAndZoom", Properties.Resources.LICENSE_PanAndZoom),
                new License("DryIoc", Properties.Resources.LICENSE_DryIoc),
                new License("Json.NET", Properties.Resources.LICENSE_JsonDotNet),
                new License("OxyPlot", Properties.Resources.LICENSE_OxyPlot),
                new License("Microsoft.VisualStudio.Threading", Properties.Resources.LICENSE_VSThreading),
                new License("StreamJsonRpc", Properties.Resources.LICENSE_StreamJsonRpc),
                new License("Math.NET Numerics", Properties.Resources.LICENSE_MathDotNet),
                new License(".NET", Properties.Resources.LICENSE_DotNet),
            };

            DataContext = licenses;
        }
    }
}
