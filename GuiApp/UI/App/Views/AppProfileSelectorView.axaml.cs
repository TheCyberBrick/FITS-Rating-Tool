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
using FitsRatingTool.GuiApp.UI.App.ViewModels;
using FitsRatingTool.GuiApp.UI.MessageBox.ViewModels;
using FitsRatingTool.GuiApp.UI.MessageBox.Windows;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.App.Views
{
    public partial class AppProfileSelectorView : ReactiveUserControl<AppProfileSelectorViewModel>
    {
        private Window? window;

        public AppProfileSelectorView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.ChangeProfileConfirmationDialog.RegisterHandler(ShowChangeProfileConfirmationDialogAsync));
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

        private async Task ShowChangeProfileConfirmationDialogAsync(InteractionContext<Unit, bool> ctx)
        {
            if (window != null)
            {
                var result = await MessageBoxWindow.ShowAsync(window, MessageBoxStyle.OkCancel, "Change profile?", "Changing the profile will invalidate all current image measurements and statistics.", null, MessageBoxIcon.Info, true);
                ctx.SetOutput(result == MessageBoxResult.Ok);
            }
            else
            {
                ctx.SetOutput(false);
            }
        }
    }
}
