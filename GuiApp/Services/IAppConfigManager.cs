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

namespace FitsRatingTool.GuiApp.Services
{
    public interface IAppConfigManager
    {
        public abstract class ValueEventArgs : EventArgs
        {
            public abstract bool IsAffected(string key);
        }

        public class ValuesReloadedEventArgs : ValueEventArgs
        {
            public override bool IsAffected(string key) => true;
        }

        public class ValueChangedEventArgs : ValueEventArgs
        {
            public string Key { get; }

            public ValueChangedEventArgs(string key)
            {
                Key = key;
            }

            public override bool IsAffected(string key) => key == Key;
        }

        bool SaveOnChange { get; set; }

        string Path { get; }

        void Load();

        void Save();

        string? Get(string key, string? fallback = null);

        void Set(string key, string? value);

        bool Contains(string key);

        event EventHandler<ValuesReloadedEventArgs> ValuesReloaded;

        event EventHandler<ValueChangedEventArgs> ValueChanged;
    }
}
