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
using System.Reactive;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.Exporters.ViewModels;

namespace FitsRatingTool.GuiApp.UI.Exporters.Views
{
    public partial class VoyagerExporterConfiguratorView : ReactiveUserControl<VoyagerExporterConfiguratorViewModel>
    {
        private Window? window;

        public VoyagerExporterConfiguratorView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.SelectCredentialsFileOpenFileDialog.RegisterHandler(ShowOpenFileDialogAsync));
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

        private async Task ShowOpenFileDialogAsync(InteractionContext<Unit, string> ctx)
        {
            if (window != null)
            {
                OpenFileDialog dialog = new()
                {
                    Filters = { new() { Name = "Credentials File", Extensions = { "json" } } }
                };

                var files = await dialog.ShowAsync(window);

                ctx.SetOutput(files != null && files.Length == 1 ? files[0] : "");
            }
            else
            {
                ctx.SetOutput("");
            }
        }
    }
}
