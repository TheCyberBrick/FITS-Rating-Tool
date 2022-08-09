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

using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.Evaluation.ViewModels;
using Avalonia;

namespace FitsRatingTool.GuiApp.UI.Evaluation.Windows
{
    public partial class EvaluationFormulaWindow : ReactiveWindow<EvaluationFormulaViewModel>
    {
        private readonly string defaultTitle;

        public EvaluationFormulaWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            defaultTitle = Title;

            this.WhenActivated(d =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.LoadFormulaOpenFileDialog.RegisterHandler(ShowOpenFileDialogAsync));
                    d.Add(ViewModel.SaveFormulaSaveFileDialog.RegisterHandler(ShowSaveFileDialogAsync));

                    d.Add(ViewModel.WhenAnyValue(x => x.LoadedFile).Subscribe(x => UpdateTitle(ViewModel)));
                    d.Add(ViewModel.WhenAnyValue(x => x.IsModified).Subscribe(x => UpdateTitle(ViewModel)));
                }
            });
        }

        private void UpdateTitle(EvaluationFormulaViewModel vm)
        {
            var fileName = vm.LoadedFile;
            var modified = vm.IsModified;
            if (fileName != null)
            {
                Title = (modified ? "*" : "") + fileName;
            }
            else
            {
                Title = defaultTitle;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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

        private async Task ShowSaveFileDialogAsync(InteractionContext<Unit, string> ctx)
        {
            SaveFileDialog dialog = new()
            {
                Filters = { new() { Name = "Text File", Extensions = { "txt" } } }
            };

            var file = await dialog.ShowAsync(this);

            ctx.SetOutput(file ?? "");
        }
    }
}
