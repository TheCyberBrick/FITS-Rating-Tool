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
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace FitsRatingTool.GuiApp.UI.Controls
{
    public class FixedContextMenu : ContextMenu
    {
        public static readonly AttachedProperty<Rect> ContextMenuRectProperty = AvaloniaProperty.RegisterAttached<FixedContextMenu, TopLevel, Rect>("ContextMenuRect", default, true, BindingMode.OneWay);

        static FixedContextMenu()
        {
            void setPosition(TopLevel s, Point pos)
            {
                var rect = new Rect(pos, new Size(1, 1));
                s.SetValue(ContextMenuRectProperty, rect);
            }

            PointerMovedEvent.AddClassHandler<TopLevel>((s, e) =>
            {
                setPosition(s, e.GetPosition(s));
            }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble | RoutingStrategies.Direct, true);

            PointerEnterEvent.AddClassHandler<TopLevel>((s, e) =>
            {
                setPosition(s, e.GetPosition(s));
            }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble | RoutingStrategies.Direct, true);

            PointerPressedEvent.AddClassHandler<TopLevel>((s, e) =>
            {
                setPosition(s, e.GetPosition(s));
            }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble | RoutingStrategies.Direct, true);
        }

        private Popup? popup;
        private IVisual? placementElement;
        private TopLevel? positionElement;

        public FixedContextMenu()
        {
            this[Popup.PlacementModeProperty] = PlacementMode.AnchorAndGravity;
            this[Popup.PlacementAnchorProperty] = PopupAnchor.TopLeft;
            this[Popup.PlacementGravityProperty] = PopupGravity.BottomRight;
        }

        private Rect GetRelativeRect(Rect r)
        {
            var p = new Point(r.Left, r.Top);
            if (positionElement != null && placementElement != null)
            {
                p = p * positionElement.TransformToVisual(placementElement) ?? default;
            }
            return new Rect(p, new Size(1, 1));
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ParentProperty)
            {
                if (popup != null)
                {
                    popup.PropertyChanged -= OnPopupPropertyChanged;
                }

                popup = change.NewValue.Value as Popup;
                placementElement = null;
                positionElement = null;

                if (popup != null)
                {
                    popup.PropertyChanged += OnPopupPropertyChanged;

                    IControl? parent = popup;

                    placementElement = popup.PlacementTarget;

                    while ((parent = parent.Parent) != null)
                    {
                        if (parent is TopLevel pe)
                        {
                            positionElement = pe;

                            this[!Popup.PlacementRectProperty] = pe[!ContextMenuRectProperty];

                            break;
                        }
                    }
                }
            }

            if (change.Property == Popup.PlacementRectProperty && change.NewValue.HasValue && popup != null && !popup.IsOpen)
            {
                popup.PlacementRect = GetRelativeRect(change.NewValue.GetValueOrDefault(default(Rect)));
            }
        }

        private void OnPopupPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
        {
            if (popup != null && sender == popup && positionElement != null && change.Property == Popup.PlacementRectProperty && change.NewValue == null)
            {
                popup.PlacementRect = GetRelativeRect(positionElement[ContextMenuRectProperty] as Rect? ?? default);
            }

            if (popup != null && popup == sender && change.Property == Popup.PlacementTargetProperty)
            {
                placementElement = change.NewValue as Control;
            }
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);

            if (popup != null)
            {
                popup.PropertyChanged -= OnPopupPropertyChanged;
            }
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);

            if (popup != null)
            {
                popup.PropertyChanged += OnPopupPropertyChanged;
            }
        }
    }
}
