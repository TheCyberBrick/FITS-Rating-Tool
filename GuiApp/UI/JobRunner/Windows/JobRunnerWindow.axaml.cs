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
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using FitsRatingTool.Common.Models.Evaluation;
using FitsRatingTool.GuiApp.UI.JobRunner.ViewModels;
using FitsRatingTool.GuiApp.UI.MessageBox.ViewModels;
using FitsRatingTool.GuiApp.UI.MessageBox.Windows;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Reactive;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.JobRunner.Windows
{
    public partial class JobRunnerWindow : ReactiveWindow<JobRunnerViewModel>
    {
        private bool hasFinished = false;

        public JobRunnerWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.WhenActivated(d =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.WhenAnyValue(x => x.Progress).Subscribe(x =>
                    {
                        if (x != null)
                        {
                            d.Add(x.ExporterConfirmationDialog.RegisterHandler(ShowExporterConfirmationDialogAsync));
                        }
                    }));
                    d.Add(ViewModel.SelectJobConfigOpenFileDialog.RegisterHandler(ShowOpenFileDialogAsync));
                    d.Add(ViewModel.SelectPathOpenFolderDialog.RegisterHandler(ShowOpenFolderDialogAsync));
                    d.Add(ViewModel.RunProgressDialog.RegisterHandler(ShowRunProgressDialogAsync));
                    d.Add(ViewModel.RunResultDialog.RegisterHandler(ShowRunResultDialogAsync));
                }
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (ViewModel != null && ViewModel.Progress != null && ViewModel.IsRunning && !hasFinished)
            {
                // Cancel closing until the task is fully cancelled
                e.Cancel = true;

                // Cancel and then close once fully cancelled
                ViewModel.Progress.Cancel.Execute().Subscribe(_ =>
                {
                    hasFinished = true;
                    Close();
                });
            }
        }

        private async Task ShowOpenFileDialogAsync(InteractionContext<Unit, string> ctx)
        {
            OpenFileDialog dialog = new()
            {
                AllowMultiple = false,
                Filters = { new() { Name = "Job Config", Extensions = { "json" } } }
            };

            var files = await dialog.ShowAsync(this);

            ctx.SetOutput(files != null && files.Length == 1 ? files[0] : "");
        }

        private async Task ShowOpenFolderDialogAsync(InteractionContext<Unit, string> ctx)
        {
            var dir = await new OpenFolderDialog().ShowAsync(this);
            ctx.SetOutput(dir ?? "");
        }

        private async Task ShowRunProgressDialogAsync(InteractionContext<IJobRunnerProgressViewModel, JobResult> ctx)
        {
            var window = new JobRunnerProgressWindow()
            {
                DataContext = ctx.Input,
                Topmost = Topmost
            };

            ctx.SetOutput(await window.ShowDialog<JobResult>(this));
        }

        private async Task ShowRunResultDialogAsync(InteractionContext<JobResult, Unit> ctx)
        {
            var result = ctx.Input;

            if (result.ErrorMessage != null)
            {
                await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.Ok, "Job Run Error", result.ErrorMessage, null, MessageBoxIcon.Error);
            }
            else if (result.Error != null)
            {
                await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.Ok, "Job Run Error", result.Error.Message, null, MessageBoxIcon.Error);
            }

            await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.Ok, "Job Run Completed", "Loaded " + result.LoadedFiles + " files and evaluated/exported " + result.ExportedFiles + " files out of " + result.NumberOfFiles + " files found in total", null, MessageBoxIcon.Info);

            ctx.SetOutput(Unit.Default);
        }

        private async Task ShowExporterConfirmationDialogAsync(InteractionContext<ConfirmationEventArgs, ConfirmationEventArgs.Result> ctx)
        {
            var e = ctx.Input;

            var result = await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.YesNo, "Do you want to proceed?", $"The '{e.RequesterName}' exporter requires confirmation: \n\r\n\r{e.Message} \n\r\n\rIf aborted, this exporter will be skipped. \nProceed?", null, MessageBoxIcon.Warning, false);

            ctx.SetOutput(result == MessageBoxResult.Yes ? ConfirmationEventArgs.Result.Proceed : ConfirmationEventArgs.Result.Abort);
        }
    }
}
