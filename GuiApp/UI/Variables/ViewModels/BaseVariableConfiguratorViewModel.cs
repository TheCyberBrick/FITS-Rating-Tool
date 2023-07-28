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

using FitsRatingTool.Common.Models.Evaluation;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.Variables.ViewModels
{
    public abstract class BaseVariableConfiguratorViewModel : ViewModelBase, IBaseVariableConfiguratorViewModel
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            protected set => this.RaiseAndSetIfChanged(ref _isValid, value);
        }

        private bool _isNameValid;
        public bool IsNameValid
        {
            get => _isNameValid;
            set => this.RaiseAndSetIfChanged(ref _isNameValid, value);
        }


        protected BaseVariableConfiguratorViewModel()
        {
            this.WhenAnyValue(x => x.Name).Skip(1).Subscribe(x => NotifyConfigurationChange());
        }

        protected void NotifyConfigurationChange()
        {
            IsValid = true;
            Validate();
            _configurationChanged?.Invoke(this, new EventArgs());
        }

        protected virtual void Validate()
        {
            IsNameValid = Name.Length > 0 && char.IsLetter(Name[0]) && Name.All(x => char.IsLetterOrDigit(x));
            IsValid = IsNameValid;
        }

        public abstract string CreateConfig();

        public abstract IVariable CreateVariable();

        public bool TryLoadConfig(string name, string config)
        {
            bool success;

            using (DelayChangeNotifications())
            {
                Name = name;
                success = DoTryLoadConfig(config);
            }

            Validate();

            return success;
        }

        protected abstract bool DoTryLoadConfig(string config);

        private EventHandler? _configurationChanged;
        public event EventHandler ConfigurationChanged
        {
            add => _configurationChanged += value;
            remove => _configurationChanged -= value;
        }
    }
}
