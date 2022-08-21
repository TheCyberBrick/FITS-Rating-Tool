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
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using FitsRatingTool.GuiApp.UI.AppConfig.ViewModels;
using ReactiveUI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.AppConfig.Views
{
    public partial class PathSettingView : ReactiveUserControl<PathSettingViewModel>
    {
        private Window? window;

        public PathSettingView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.SelectPathOpenDialog.RegisterHandler(ShowOpenDialogAsync));
                }
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            window = e.Root as Window;
            base.OnAttachedToLogicalTree(e);
        }

        private async Task ShowOpenDialogAsync(InteractionContext<IPathProperties, string> ctx)
        {
            if (window != null)
            {
                var pathProperties = ctx.Input;

                if (pathProperties.AllowFilePath)
                {
                    OpenFileDialog dialog = new()
                    {
                        Title = pathProperties.Name,
                        Filters = { new() { Extensions = new List<string>(pathProperties.AllowedFileExtensions) } }
                    };

                    var files = await dialog.ShowAsync(window);

                    ctx.SetOutput(files != null && files.Length == 1 ? files[0] : "");
                }
                else
                {
                    OpenFolderDialog dialog = new()
                    {
                        Title = pathProperties.Name
                    };

                    var folder = await dialog.ShowAsync(window);

                    ctx.SetOutput(folder != null ? folder : "");
                }
            }
            else
            {
                ctx.SetOutput("");
            }
        }
    }
}
