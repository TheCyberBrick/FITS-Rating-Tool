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
using System;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.Models
{
    public class ConfigSetting<T> : ReactiveObject, IConfigSetting
    {
        private T? modifiedValue = default;

        public T Value
        {
            get
            {
                return IsModified ? modifiedValue! : getter();
            }
            set
            {
                bool change = !EqualityComparer<T>.Default.Equals(value, Value);
                if (change) this.RaisePropertyChanging(nameof(Value));
                if (!EqualityComparer<T>.Default.Equals(value, getter()))
                {
                    modifiedValue = value;
                    IsModified = true;
                }
                else
                {
                    modifiedValue = default;
                    IsModified = false;
                }
                if (change) this.RaisePropertyChanged(nameof(Value));
            }
        }

        object? IConfigSetting.Value
        {
            get => Value;
            set
            {
                if (value is T)
                {
                    Value = (T)value;
                }
            }
        }

        private bool _modified;
        public bool IsModified
        {
            get => _modified;
            private set => this.RaiseAndSetIfChanged(ref _modified, value);
        }

        private readonly Func<T> getter;
        private readonly Action<T> setter;

        public ConfigSetting(Func<T> getter, Action<T> setter)
        {
            this.getter = getter;
            this.setter = setter;

            Reset();
        }

        public void Reset()
        {
            modifiedValue = default;
            IsModified = false;
        }

        public void Commit()
        {
            if (IsModified)
            {
                setter(modifiedValue!);
                Reset();
            }
        }
    }
}
