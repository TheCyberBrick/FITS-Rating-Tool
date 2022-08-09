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
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace FitsRatingTool.GuiApp.UI.Controls.ContextualItemsControl
{
    public class ContextContainerTemplate<T> : ITemplate<T> where T : IContextContainer
    {
#pragma warning disable CS8618, CS8600, CS8603
        [Content]
        [TemplateContent]
        public object Content { get; set; }


        public T Build() => (T)TemplateContent.Load(Content)?.Control;

        object ITemplate.Build() => Build();
#pragma warning restore CS8618, CS8600, CS8603
    }
}