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
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

namespace FitsRatingTool.GuiApp.UI.Controls
{
    public class ScrollableTabControl : TabControl
    {
        public static DirectProperty<ScrollableTabControl, bool> IsOverflowingProperty = AvaloniaProperty.RegisterDirect<ScrollableTabControl, bool>(nameof(IsOverflowing), o => o.IsOverflowing);

        public static StyledProperty<bool> QuickSelectDropdownProperty = AvaloniaProperty.Register<ScrollableTabControl, bool>(nameof(QuickSelectDropdown), true);

        private bool _isOverflowing;
        public bool IsOverflowing
        {
            get => _isOverflowing;
            private set => SetAndRaise(IsOverflowingProperty, ref _isOverflowing, value);
        }

        public bool QuickSelectDropdown
        {
            get => GetValue(QuickSelectDropdownProperty);
            set => SetValue(QuickSelectDropdownProperty, value);
        }

        private ScrollViewer? tabScrollViewer;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (tabScrollViewer != null)
            {
                tabScrollViewer.ScrollChanged -= OnScrollChanged;
            }

            tabScrollViewer = e.NameScope.Get<ScrollViewer>("PART_TabScrollViewer");

            tabScrollViewer.ScrollChanged += OnScrollChanged;
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);

            if (tabScrollViewer != null)
            {
                tabScrollViewer.ScrollChanged += OnScrollChanged;
            }
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);

            if (tabScrollViewer != null)
            {
                tabScrollViewer.ScrollChanged -= OnScrollChanged;
            }
        }

        private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (tabScrollViewer != null)
            {
                IsOverflowing = tabScrollViewer.Extent.Width > tabScrollViewer.Viewport.Width;
            }
        }

        public void ScrollRight()
        {
            if (tabScrollViewer != null)
            {
                tabScrollViewer.Offset = tabScrollViewer.Offset.WithX(tabScrollViewer.Offset.X + tabScrollViewer.Bounds.Width * 0.5);
            }
        }

        public void ScrollLeft()
        {
            if (tabScrollViewer != null)
            {
                tabScrollViewer.Offset = tabScrollViewer.Offset.WithX(tabScrollViewer.Offset.X - tabScrollViewer.Bounds.Width * 0.5);
            }
        }

        public void ScrollTo(object? item)
        {
            if (tabScrollViewer != null)
            {
                int i = IndexOf(Items, item);
                if (i >= 0)
                {
                    SelectedIndex = i;
                }
            }
        }
    }
}
