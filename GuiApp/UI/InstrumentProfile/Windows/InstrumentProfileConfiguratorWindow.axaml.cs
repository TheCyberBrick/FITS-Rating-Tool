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

using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia;
using Avalonia.ReactiveUI;
using FitsRatingTool.GuiApp.UI.InstrumentProfile.ViewModels;
using ReactiveUI;
using System.Threading.Tasks;
using FitsRatingTool.Common.Models.Instrument;
using FitsRatingTool.GuiApp.UI.MessageBox.Windows;
using FitsRatingTool.GuiApp.UI.MessageBox.ViewModels;
using System.Reactive;
using System;
using System.ComponentModel;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.InstrumentProfile.Windows
{
    public partial class InstrumentProfileConfiguratorWindow : ReactiveWindow<InstrumentProfileConfiguratorViewModel>
    {
        private bool doClose = false;

        public InstrumentProfileConfiguratorWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.WhenActivated(d =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.DeleteConfirmationDialog.RegisterHandler(ShowDeleteConfirmationDialogAsync));
                    d.Add(ViewModel.DiscardConfirmationDialog.RegisterHandler(ShowCancelConfirmationDialogAsync));

                    d.Add(ViewModel.Cancel.Subscribe(cancel =>
                    {
                        if (cancel)
                        {
                            doClose = true;
                            Close();
                        }
                    }));

                    d.Add(ViewModel.ImportOpenFileDialog.RegisterHandler(ShowImportOpenFileDialogAsync));
                    d.Add(ViewModel.ExportSaveFileDialog.RegisterHandler(ShowExportSaveFileDialogAsync));

                    d.Add(ViewModel.ImportResultDialog.RegisterHandler(ShowImportResultDialogAsync));
                    d.Add(ViewModel.ExportResultDialog.RegisterHandler(ShowExportResultDialogAsync));
                }
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            var vm = ViewModel;

            if (!doClose && vm != null)
            {
                e.Cancel = true;

                async void checkCloseAsync()
                {
                    var cancel = await vm.Cancel.Execute(Unit.Default);

                    if (cancel)
                    {
                        doClose = true;
                        Close();
                    }
                }

                checkCloseAsync();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task ShowDeleteConfirmationDialogAsync(InteractionContext<IReadOnlyInstrumentProfile, bool> ctx)
        {
            var profile = ctx.Input;

            var result = await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.OkCancel, "Are you sure?", $"This will delete the profile '{profile.Name} ({profile.Id})' permanently!", null, MessageBoxIcon.Warning, false);

            ctx.SetOutput(result == MessageBoxResult.Ok);
        }

        private async Task ShowCancelConfirmationDialogAsync(InteractionContext<Unit, bool> ctx)
        {
            var result = await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.OkCancel, "Discard changes?", $"The currently selected profile was changed. Are you sure you want to discard the changes?", null, MessageBoxIcon.Warning, false);

            ctx.SetOutput(result == MessageBoxResult.Ok);
        }

        private async Task ShowImportOpenFileDialogAsync(InteractionContext<Unit, string> ctx)
        {
            OpenFileDialog dialog = new()
            {
                AllowMultiple = false,
                Filters = { new() { Name = "Profile", Extensions = { "json" } } }
            };

            var files = await dialog.ShowAsync(this);

            ctx.SetOutput(files != null && files.Length == 1 ? files[0] : "");
        }

        private async Task ShowExportSaveFileDialogAsync(InteractionContext<Unit, string> ctx)
        {
            SaveFileDialog dialog = new()
            {
                Filters = { new() { Name = "Profile", Extensions = { "json" } } }
            };

            var file = await dialog.ShowAsync(this);

            ctx.SetOutput(file ?? "");
        }

        private async Task ShowImportResultDialogAsync(InteractionContext<IInstrumentProfileConfiguratorViewModel.ImportResult, Unit> ctx)
        {
            var result = ctx.Input;

            if (result.ErrorMessage != null)
            {
                await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.Ok, "Import Error", result.ErrorMessage, null, MessageBoxIcon.Error);
            }
            else if (result.Error != null)
            {
                await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.Ok, "Import Error", result.Error.Message, null, MessageBoxIcon.Error);
            }
            else if (result.Successful)
            {
                await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.Ok, "Import Completed", $"Successfully imported profile '{result.Profile?.Name} ({result.Profile?.Id})'", null, MessageBoxIcon.Info);
            }

            ctx.SetOutput(Unit.Default);
        }

        private async Task ShowExportResultDialogAsync(InteractionContext<IInstrumentProfileConfiguratorViewModel.ExportResult, Unit> ctx)
        {
            var result = ctx.Input;

            if (result.ErrorMessage != null)
            {
                await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.Ok, "Export Error", result.ErrorMessage, null, MessageBoxIcon.Error);
            }
            else if (result.Error != null)
            {
                await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.Ok, "Export Error", result.Error.Message, null, MessageBoxIcon.Error);
            }
            else if (result.Successful)
            {
                await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.Ok, "Export Completed", $"Successfully exported profile", null, MessageBoxIcon.Info);
            }

            ctx.SetOutput(Unit.Default);
        }
    }
}
