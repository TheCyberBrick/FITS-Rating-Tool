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
using FitsRatingTool.GuiApp.UI.MessageBox.ViewModels;
using FitsRatingTool.GuiApp.UI.MessageBox.Windows;
using MathNet.Numerics;

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
                    d.Add(ViewModel.ResetConfirmationDialog.RegisterHandler(ShowResetConfirmationDialogAsync));

                    d.Add(ViewModel.WhenAnyValue(x => x.LoadedFile).Subscribe(x => UpdateTitle(ViewModel)));
                    d.Add(ViewModel.WhenAnyValue(x => x.LoadedInstrumentProfile).Subscribe(x => UpdateTitle(ViewModel)));
                    d.Add(ViewModel.WhenAnyValue(x => x.IsModified).Subscribe(x => UpdateTitle(ViewModel)));
                }
            });
        }

        private void UpdateTitle(EvaluationFormulaViewModel vm)
        {
            var fileName = vm.LoadedFile;
            var profileName = vm.LoadedInstrumentProfile?.Name;
            var modified = vm.IsModified;

            var prefix = modified ? "*" : "";

            if (fileName != null)
            {
                Title = prefix + fileName;
            }
            else if (profileName != null)
            {
                Title = prefix + defaultTitle + " (Profile: " + profileName + ")";
            }
            else
            {
                Title = prefix + defaultTitle;
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

        private async Task ShowResetConfirmationDialogAsync(InteractionContext<Unit, bool> ctx)
        {
            var result = await MessageBoxWindow.ShowAsync(this, MessageBoxStyle.OkCancel, "Reset?", "The evaluation grouping and formula will be reset.\nYour changes here will be lost.\nContinue?", null, MessageBoxIcon.Warning, true);
            ctx.SetOutput(result == MessageBoxResult.Ok);
        }
    }
}
