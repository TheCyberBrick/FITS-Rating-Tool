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
using Avalonia.Layout;
using Avalonia.Media;

namespace FitsRatingTool.GuiApp.UI.Controls
{
    public class DataGridSelectableTextColumn : DataGridTextColumn
    {
        public static readonly StyledProperty<Thickness> TextBoxMarginProperty = AvaloniaProperty.Register<DataGridSelectableTextColumn, Thickness>(nameof(TextBoxMargin), new Thickness(1, 2, -1000 /* prevents column resizing when selecting text */, -2));

        public static readonly StyledProperty<string> TextBoxClassesProperty = AvaloniaProperty.Register<DataGridSelectableTextColumn, string>(nameof(TextBoxClasses), "readonly");

        public Thickness TextBoxMargin
        {
            get => GetValue(TextBoxMarginProperty);
            set => SetValue(TextBoxMarginProperty, value);
        }

        public string TextBoxClasses
        {
            get => GetValue(TextBoxClassesProperty);
            set => SetValue(TextBoxClassesProperty, value);
        }

        public DataGridSelectableTextColumn()
        {
            // Required for generating the editing element
            IsReadOnly = false;
        }

        protected override IControl GenerateEditingElementDirect(DataGridCell cell, object dataItem)
        {
            var textBox = new TextBox
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Colors.Transparent),
                IsReadOnly = true,
                Margin = TextBoxMargin,
                Classes = Classes.Parse(TextBoxClasses)
            };

            SyncProperties(textBox);

            return textBox;
        }

        private void SyncProperties(AvaloniaObject content)
        {
            SyncColumnProperty(this, content, FontFamilyProperty);
            SyncColumnProperty(this, content, FontSizeProperty);
            SyncColumnProperty(this, content, FontStyleProperty);
            SyncColumnProperty(this, content, FontWeightProperty);
            SyncColumnProperty(this, content, ForegroundProperty);
        }

        private static void SyncColumnProperty<T>(AvaloniaObject column, AvaloniaObject content, AvaloniaProperty<T> property)
        {
            SyncColumnProperty(column, content, property, property);
        }

        private static void SyncColumnProperty<T>(AvaloniaObject column, AvaloniaObject content, AvaloniaProperty<T> contentProperty, AvaloniaProperty<T> columnProperty)
        {
            if (!column.IsSet(columnProperty))
            {
                content.ClearValue(contentProperty);
            }
            else
            {
                content.SetValue(contentProperty, column.GetValue(columnProperty));
            }
        }
    }
}
