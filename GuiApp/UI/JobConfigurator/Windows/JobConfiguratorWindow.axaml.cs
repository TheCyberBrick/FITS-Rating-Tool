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
using Avalonia.ReactiveUI;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.JobConfigurator.ViewModels;
using FitsRatingTool.GuiApp.UI.MessageBox.Windows;
using FitsRatingTool.GuiApp.UI.MessageBox.ViewModels;
using Avalonia;

namespace FitsRatingTool.GuiApp.UI.JobConfigurator.Windows
{
    public partial class JobConfiguratorWindow : ReactiveWindow<JobConfiguratorViewModel>
    {
        public JobConfiguratorWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.WhenActivated((CompositeDisposable d) =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.EvaluationFormula.LoadFormulaOpenFileDialog.RegisterHandler(ShowOpenFileDialogAsync));
                    d.Add(ViewModel.SelectOutputLogsPathOpenFolderDialog.RegisterHandler(ShowOpenFolderDialogAsync));
                    d.Add(ViewModel.SelectCachePathOpenFolderDialog.RegisterHandler(ShowOpenFolderDialogAsync));
                    d.Add(ViewModel.SaveJobConfigSaveFileDialog.RegisterHandler(ShowSaveFileDialogAsync));
                    d.Add(ViewModel.SaveJobConfigResultDialog.RegisterHandler(ShowSaveJobConfigResultDialogAsync));
                }
            });
        }

        private async Task ShowOpenFileDialogAsync(InteractionContext<Unit, string> ctx)
        {
            OpenFileDialog dialog = new()
            {
                AllowMultiple = false,
                Filters = { new() { Name = "Text File", Extensions = { "txt" } }, new() { Name = "Text File", Extensions = { "*" } } }
            };

            var files = await dialog.ShowAsync(this);

            ctx.SetOutput(files != null && files.Length == 1 ? files[0] : "");
        }

        private async Task ShowOpenFolderDialogAsync(InteractionContext<Unit, string> ctx)
        {
            var dir = await new OpenFolderDialog().ShowAsync(this);
            ctx.SetOutput(dir ?? "");
        }

        private async Task ShowSaveFileDialogAsync(InteractionContext<Unit, string> ctx)
        {
            SaveFileDialog dialog = new()
            {
                Filters = { new() { Name = "Job Config", Extensions = { "json" } } }
            };

            var file = await dialog.ShowAsync(this);

            ctx.SetOutput(file ?? "");
        }

        private async Task ShowSaveJobConfigResultDialogAsync(InteractionContext<IJobConfiguratorViewModel.SaveResult, Unit> ctx)
        {
            var result = ctx.Input;

            if (result.Error != null)
            {
                await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.Ok, "Job Configuration Error", result.Error.Message, null, MessageBoxIcon.Error);
            }

            ctx.SetOutput(Unit.Default);
        }
    }
}
