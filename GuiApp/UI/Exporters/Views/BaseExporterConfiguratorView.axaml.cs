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
using ReactiveUI;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.Exporters.ViewModels;

namespace FitsRatingTool.GuiApp.UI.Exporters.Views
{
    public partial class BaseExporterConfiguratorView : ReactiveUserControl<BaseExporterConfiguratorViewModel>
    {
        private Window? window;

        public BaseExporterConfiguratorView()
        {
            AvaloniaXamlLoader.Load(this);

            this.WhenActivated(d =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.SelectPathSaveFileDialog.RegisterHandler(ShowSaveFileDialogAsync));
                }
            });
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            window = e.Root as Window;
            base.OnAttachedToLogicalTree(e);
        }

        private async Task ShowSaveFileDialogAsync(InteractionContext<IBaseExporterConfiguratorViewModel.FileExtension, string> ctx)
        {
            if (window != null)
            {
                var ext = ctx.Input;

                SaveFileDialog dialog = new()
                {
                    Filters = { new() { Name = ext.Name, Extensions = { ext.Extension } } }
                };

                var file = await dialog.ShowAsync(window);

                ctx.SetOutput(file ?? "");
            }
            else
            {
                ctx.SetOutput("");
            }
        }
    }
}
