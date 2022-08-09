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
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using System;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.UI.Controls.ContextualItemsControl
{
    public abstract class ContextualItemsControl<TContext, TDefaultContext> : SelectingItemsControl
        where TContext : class, IContextContainer
        where TDefaultContext : TContext, new()
    {
        protected class ContextualItemContainerGenerator : ItemContainerGenerator
        {
            private readonly ContextualItemsControl<TContext, TDefaultContext> control;

            public ContextualItemContainerGenerator(ContextualItemsControl<TContext, TDefaultContext> control) : base(control)
            {
                this.control = control;
            }

            protected override IControl CreateContainer(object item)
            {
                var result = item as TContext;

                if (result == null)
                {
                    result = control.ContextContainerTemplate.Build();
                    result.DataContext = item;
                    result.SetValue(ContentPresenter.ContentProperty, item, BindingPriority.Style);

                    if (ItemTemplate != null)
                    {
                        result.SetValue(
                            ContentPresenter.ContentTemplateProperty,
                            ItemTemplate,
                            BindingPriority.TemplatedParent);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// The default value for the <see cref="ContextContainerTemplate"/> property.
        /// </summary>
        private static readonly FuncTemplate<TContext> DefaultContextContainer = new(() => new TDefaultContext());

        /// <summary>
        /// Defines the <see cref="ContextContainerTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<TContext>> ContextContainerTemplateProperty = AvaloniaProperty.Register<ContextualItemsControl<TContext, TDefaultContext>, ITemplate<TContext>>(nameof(ContextContainerTemplate), DefaultContextContainer);

        /// <summary>
        /// Gets or sets the container used to present and hold the context of the items.
        /// </summary>
        public ITemplate<TContext> ContextContainerTemplate
        {
            get { return GetValue(ContextContainerTemplateProperty); }
            set { SetValue(ContextContainerTemplateProperty, value); }
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ContextualItemContainerGenerator(this);
        }

        private Dictionary<TContext, EventHandler<AvaloniaPropertyChangedEventArgs>> propertyChangedHandlers = new();

        protected override void OnContainersMaterialized(ItemContainerEventArgs e)
        {
            foreach (var container in e.Containers)
            {
                if (container.ContainerControl is TContext obj)
                {
                    void handler(object? sender, AvaloniaPropertyChangedEventArgs args)
                    {
                        OnContainerPropertyChanged(obj, args);
                    }
                    propertyChangedHandlers.Add(obj, handler);
                    obj.PropertyChanged += handler;
                }
            }

            base.OnContainersMaterialized(e);
        }

        protected override void OnContainersDematerialized(ItemContainerEventArgs e)
        {
            foreach (var container in e.Containers)
            {
                if (container.ContainerControl is TContext obj)
                {
                    if (propertyChangedHandlers.Remove(obj, out var handler))
                    {
                        obj.PropertyChanged -= handler;
                    }
                }
            }

            base.OnContainersDematerialized(e);
        }

        private void OnContainerPropertyChanged(TContext container, AvaloniaPropertyChangedEventArgs args)
        {
            OnContainerPropertyChanged(container.DataContext, ItemContainerGenerator.IndexFromContainer(container), container, args);
        }

        protected virtual void OnContainerPropertyChanged(object? item, int index, TContext context, AvaloniaPropertyChangedEventArgs args)
        {
        }

        public struct ContextRecord
        {
            public object? Item;
            public int Index;
            public TContext Context;
        }

        public IEnumerable<ContextRecord> GetContextRecords()
        {
            foreach (var child in LogicalChildren)
            {
                if (child is TContext container)
                {
                    yield return new ContextRecord
                    {
                        Item = container.DataContext,
                        Index = ItemContainerGenerator.IndexFromContainer(container),
                        Context = container
                    };
                }
            }
        }
    }
}
