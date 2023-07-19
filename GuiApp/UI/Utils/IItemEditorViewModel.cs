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

using FitsRatingTool.IoC;
using System;

namespace FitsRatingTool.GuiApp.UI.Utils
{
    public interface IItemEditorViewModel<TConfigurator>
        where TConfigurator : class, IItemConfigurator
    {
        bool IsValid { get; }

        IItemSelectorViewModel Selector { get; }

        TConfigurator? Configurator { get; }

        void SetConfigurator(IDelegatedFactory<TConfigurator>? factory);

        void Reset();

        event EventHandler ConfigurationChanged;
    }

    public static class IItemEditorViewModelExtensions
    {
        public static bool Configure<TConfigurator>(this IItemEditorViewModel<TConfigurator> editor, string id, Func<TConfigurator, bool> configuration)
            where TConfigurator : class, IItemConfigurator
        {
            var item = editor.Selector.SelectById(id);
            if (item != null && editor.Selector.SelectedItem == item)
            {
                var configurator = editor.Configurator;
                if (configurator != null)
                {
                    return configuration.Invoke(configurator);
                }
            }
            return false;
        }
    }
}
