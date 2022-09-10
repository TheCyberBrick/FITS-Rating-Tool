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
using FitsRatingTool.GuiApp.UI.MessageBox.ViewModels;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.UI.MessageBox.Windows
{
    public partial class MessageBoxWindow : ReactiveWindow<MessageBoxViewModel>
    {
        private bool hasResult = false;

        private MessageBoxWindow(MessageBoxViewModel? vm)
        {
            DataContext = vm ?? new MessageBoxViewModel();

            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.WhenActivated(d =>
            {
                if (ViewModel != null)
                {
                    d.Add(ViewModel.Close.Subscribe(r =>
                    {
                        hasResult = true;
                        Close(r);
                    }));
                }
            });
        }

        public MessageBoxWindow() : this(null)
        {
        }

        public static async void Show(Window owner, MessageBoxStyle style, string title, string message, string? header = null, MessageBoxIcon icon = MessageBoxIcon.None, bool closeable = true)
        {
            await ShowAsync(owner, style, title, message, header, icon, closeable);
        }

        public static Task<MessageBoxResult> ShowAsync(Window owner, MessageBoxStyle style, string title, string message, string? header = null, MessageBoxIcon icon = MessageBoxIcon.None, bool closeable = true)
        {
            var msg = new MessageBoxWindow(new MessageBoxViewModel(style, title, message, header, icon, closeable));
            return msg.ShowDialog<MessageBoxResult>(owner);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == OwnerProperty && change.NewValue.Value is WindowBase w && w.Topmost)
            {
                // If parent is topmost then so must the message box, otherwise
                // the user may not be able to see or interact with the dialog
                Topmost = true;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!hasResult && !e.Cancel)
            {
                e.Cancel = true;
                hasResult = true;
                Close(MessageBoxResult.None);
            }
        }
    }
}
