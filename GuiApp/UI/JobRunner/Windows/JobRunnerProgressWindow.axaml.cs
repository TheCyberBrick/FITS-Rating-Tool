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
using ReactiveUI;
using System;
using System.ComponentModel;
using FitsRatingTool.GuiApp.UI.JobRunner.ViewModels;
using FitsRatingTool.Common.Models.Evaluation;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.MessageBox.Windows;
using FitsRatingTool.GuiApp.UI.MessageBox.ViewModels;

namespace FitsRatingTool.GuiApp.UI.JobRunner.Windows
{
    public partial class JobRunnerProgressWindow : ReactiveWindow<JobRunnerProgressViewModel>
    {
        private bool hasFinished = false;

        public JobRunnerProgressWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.WhenActivated((d) =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.ExporterConfirmationDialog.RegisterHandler(ShowExporterConfirmationDialogAsync));

                    d.Add(ViewModel.Run.Execute().Subscribe(args =>
                    {
                        hasFinished = true;
                        Close(args.Value);
                    }));
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

            if (ViewModel != null && !hasFinished)
            {
                ViewModel.SetCancelling();

                // Cancel closing until the task is fully cancelled
                e.Cancel = true;
            }
        }

        private async Task ShowExporterConfirmationDialogAsync(InteractionContext<ConfirmationEventArgs, ConfirmationEventArgs.Result> ctx)
        {
            var e = ctx.Input;

            var result = await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.YesNo, "Do you want to proceed?", $"The '{e.RequesterName}' exporter requires confirmation: \n\r\n\r{e.Message} \n\r\n\rIf aborted, this exporter will be skipped. \nProceed?", null, MessageBoxIcon.Warning, false);

            ctx.SetOutput(result == MessageBoxResult.Yes ? ConfirmationEventArgs.Result.Proceed : ConfirmationEventArgs.Result.Abort);
        }
    }
}
