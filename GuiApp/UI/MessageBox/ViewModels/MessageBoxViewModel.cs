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

using ReactiveUI;

namespace FitsRatingTool.GuiApp.UI.MessageBox.ViewModels
{
    public enum MessageBoxResult
    {
        Ok, Cancel, Yes, No, Abort, Retry, Ignore, None
    }

    public enum MessageBoxStyle
    {
        Ok, OkCancel, YesNo, YesNoCancel, AbortRetryIgnore
    }

    public enum MessageBoxIcon
    {
        None, Info, Question, Warning, Error
    }

    public class MessageBoxViewModel : ViewModelBase
    {
        public ReactiveCommand<MessageBoxResult, MessageBoxResult> Close { get; }


        private MessageBoxStyle _style;
        public MessageBoxStyle Style
        {
            get => _style;
            set => this.RaiseAndSetIfChanged(ref _style, value);
        }

        private readonly ObservableAsPropertyHelper<bool> _isOkStyle;
        public bool IsOkStyle
        {
            get => _isOkStyle.Value;
        }

        private readonly ObservableAsPropertyHelper<bool> _isOkCancelStyle;
        public bool IsOkCancelStyle
        {
            get => _isOkCancelStyle.Value;
        }

        private readonly ObservableAsPropertyHelper<bool> _isYesNoStyle;
        public bool IsYesNoStyle
        {
            get => _isYesNoStyle.Value;
        }

        private readonly ObservableAsPropertyHelper<bool> _isYesNoCancelStyle;
        public bool IsYesNoCancelStyle
        {
            get => _isYesNoCancelStyle.Value;
        }

        private readonly ObservableAsPropertyHelper<bool> _isAbortRetryIgnoreStyle;
        public bool IsAbortRetryIgnoreStyle
        {
            get => _isAbortRetryIgnoreStyle.Value;
        }

        private string _title = "";
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        private string _message = "";
        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        private string? _header = null;
        public string? Header
        {
            get => _header;
            set => this.RaiseAndSetIfChanged(ref _header, value);
        }

        private MessageBoxIcon _icon;
        public MessageBoxIcon Icon
        {
            get => _icon;
            set => this.RaiseAndSetIfChanged(ref _icon, value);
        }

        private readonly ObservableAsPropertyHelper<bool> _isInfo;
        public bool IsInfo
        {
            get => _isInfo.Value;
        }

        private readonly ObservableAsPropertyHelper<bool> _isQuestion;
        public bool IsQuestion
        {
            get => _isQuestion.Value;
        }

        private readonly ObservableAsPropertyHelper<bool> _isWarning;
        public bool IsWarning
        {
            get => _isWarning.Value;
        }

        private readonly ObservableAsPropertyHelper<bool> _isError;
        public bool IsError
        {
            get => _isError.Value;
        }

        private bool _isCloseable;
        public bool IsCloseable
        {
            get => _isCloseable;
            set => this.RaiseAndSetIfChanged(ref _isCloseable, value);
        }

        public MessageBoxViewModel() : this(MessageBoxStyle.Ok, "", "", "", MessageBoxIcon.None, true)
        {
        }

        public MessageBoxViewModel(MessageBoxStyle style, string title, string message, string? header, MessageBoxIcon icon, bool closeable)
        {
            Style = style;
            Title = title;
            Message = message;
            Header = header;
            Icon = icon;
            IsCloseable = closeable;

            Close = ReactiveCommand.Create<MessageBoxResult, MessageBoxResult>(r => r);

            _isOkStyle = this.WhenAnyValue(x => x.Style, x => x == MessageBoxStyle.Ok).ToProperty(this, x => x.IsOkStyle);
            _isOkCancelStyle = this.WhenAnyValue(x => x.Style, x => x == MessageBoxStyle.OkCancel).ToProperty(this, x => x.IsOkCancelStyle);
            _isYesNoStyle = this.WhenAnyValue(x => x.Style, x => x == MessageBoxStyle.YesNo).ToProperty(this, x => x.IsYesNoStyle);
            _isYesNoCancelStyle = this.WhenAnyValue(x => x.Style, x => x == MessageBoxStyle.YesNoCancel).ToProperty(this, x => x.IsYesNoCancelStyle);
            _isAbortRetryIgnoreStyle = this.WhenAnyValue(x => x.Style, x => x == MessageBoxStyle.AbortRetryIgnore).ToProperty(this, x => x.IsAbortRetryIgnoreStyle);

            _isInfo = this.WhenAnyValue(x => x.Icon, x => x == MessageBoxIcon.Info).ToProperty(this, x => x.IsInfo);
            _isQuestion = this.WhenAnyValue(x => x.Icon, x => x == MessageBoxIcon.Question).ToProperty(this, x => x.IsQuestion);
            _isWarning = this.WhenAnyValue(x => x.Icon, x => x == MessageBoxIcon.Warning).ToProperty(this, x => x.IsWarning);
            _isError = this.WhenAnyValue(x => x.Icon, x => x == MessageBoxIcon.Error).ToProperty(this, x => x.IsError);
        }
    }
}
