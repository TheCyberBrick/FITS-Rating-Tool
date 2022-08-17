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
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using FitsRatingTool.GuiApp.UI.AppConfig.ViewModels;
using ReactiveUI;
using System;

namespace FitsRatingTool.GuiApp.UI.AppConfig.Windows
{
    public partial class AppConfigWindow : ReactiveWindow<AppConfigViewModel>
    {
        public AppConfigWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.WhenActivated((d) =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.SaveAndExit.Subscribe(_ => Close()));
                    d.Add(ViewModel.Cancel.Subscribe(_ => Close()));
                }
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
