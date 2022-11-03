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

using System.Collections.Specialized;
using System.Reactive.Disposables;

namespace FitsRatingTool.IoC
{
    public static class IContainerExtensions
    {
        public static Instance GetAny<Instance, Parameter>(this IContainer<Instance, Parameter> container)
            where Instance : class
        {
            return container.FirstOrDefault() ?? throw new InvalidOperationException("No instance");
        }
        public static Instance? GetAnyOrDefault<Instance, Parameter>(this IContainer<Instance, Parameter> container)
            where Instance : class
        {
            return container.FirstOrDefault();
        }

        public static void Inject<Instance, Parameter>(this IContainer<Instance, Parameter> container, Parameter parameter, Action<Instance>? consumer = null)
            where Instance : class
        {
            void activate()
            {
                var instance = container.Instantiate(parameter);
                consumer?.Invoke(instance);
            }
            if (!container.IsInitialized)
            {
                void onInitialized()
                {
                    container.OnInitialized -= onInitialized;
                    activate();
                }
                container.OnInitialized += onInitialized;
            }
            else
            {
                activate();
            }
        }

        public static IDisposable BindTo<Instance, Parameter>(this IContainer<Instance, Parameter> container, ICollection<Instance> collection)
            where Instance : class
        {
            void onCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {

                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    collection.Clear();
                }
                else if ((e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace) && e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        collection.Remove((Instance)item);
                    }
                }

                if ((e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Reset) && e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        collection.Add((Instance)item);
                    }
                }
            }

            container.CollectionChanged += onCollectionChanged;

            return Disposable.Create(() => container.CollectionChanged -= onCollectionChanged);
        }

        public static IDisposable DestroyWithDisposable<Instance, Parameter>(this IContainer<Instance, Parameter> container, Instance instance)
            where Instance : class
        {
            return Disposable.Create(() => container.Destroy(instance));
        }
    }
}
