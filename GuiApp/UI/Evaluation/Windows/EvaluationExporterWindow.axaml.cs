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
using FitsRatingTool.GuiApp.UI.Evaluation.ViewModels;
using FitsRatingTool.GuiApp.UI.MessageBox.ViewModels;
using FitsRatingTool.GuiApp.UI.MessageBox.Windows;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.Evaluation.Windows
{
    public partial class EvaluationExporterWindow : ReactiveWindow<EvaluationExporterViewModel>
    {
        public EvaluationExporterWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.WhenActivated(d =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.ExportProgressDialog.RegisterHandler(ShowExportProgressDialogAsync));
                    d.Add(ViewModel.ExportResultDialog.RegisterHandler(ShowExportResultDialogAsync));
                }
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task ShowExportProgressDialogAsync(InteractionContext<IEvaluationExportProgressViewModel, ExportResult> ctx)
        {
            var window = new EvaluationExportProgressWindow()
            {
                DataContext = ctx.Input,
                Topmost = Topmost
            };

            ctx.SetOutput(await window.ShowDialog<ExportResult>(this));
        }

        private async Task ShowExportResultDialogAsync(InteractionContext<ExportResult, Unit> ctx)
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

            await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.Ok, "Export Completed", "Exported evaluation for " + result.NumExported + "/" + result.NumFiles + " files", null, MessageBoxIcon.Info);

            ctx.SetOutput(Unit.Default);
        }
    }
}
