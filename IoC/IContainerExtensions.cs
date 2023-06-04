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

using System.Collections;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reflection;

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

        public static void Inject<Instance, Parameter, Target>(this IContainer<Instance, Parameter> container, Parameter parameter, Target target, Expression<Func<Target, Instance>> targetPropertyExpr)
            where Instance : class
        {
            if (targetPropertyExpr.Body is MemberExpression expr && expr.Member is PropertyInfo property)
            {
                Inject(container, parameter, instance => property.SetValue(target, instance));
            }
            else
            {
                throw new ArgumentException("Invalid target property expression. Expected: x => x.Property");
            }
        }

        public static Lazy<Instance> Lazy<Instance, Parameter>(this IContainer<Instance, Parameter> container, Parameter parameter)
            where Instance : class
        {
            return new Lazy<Instance>(() => container.Instantiate(parameter));
        }

        public static IDisposable BindTo<Instance, Parameter, Target>(this ISingletonContainer<Instance, Parameter> container, Target target, Expression<Func<Target, Instance>> targetPropertyExpr)
            where Instance : class
        {
            if (!container.IsSingleton)
            {
                throw new ArgumentException("Container is not a singleton container");
            }

            if (targetPropertyExpr.Body is MemberExpression expr && expr.Member is PropertyInfo property)
            {
                return container.Singleton().Subscribe(instance => property.SetValue(target, instance));
            }
            else
            {
                throw new ArgumentException("Invalid target property expression. Expected: x => x.Property");
            }
        }

        public static IDisposable BindTo<Instance, Parameter>(this IContainer<Instance, Parameter> container, ICollection<Instance> collection, bool byIndex = false)
            where Instance : class
        {
            void onCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                Func<int, object?>? get = null;
                Action<int, object?>? insert = null;
                Action<int, object?>? set = null;
                Action<int>? remove = null;

                if (collection is IList<Instance> ilist1)
                {
                    get = i => ilist1[i];
                    insert = (i, o) => ilist1.Insert(i, (Instance)o!);
                    set = (i, o) => ilist1[i] = (Instance)o!;
                    remove = ilist1.RemoveAt;
                }
                else if (collection is IList ilist2)
                {
                    get = i => ilist2[i];
                    insert = ilist2.Insert;
                    set = (i, o) => ilist2[i] = o;
                    remove = ilist2.RemoveAt;
                }

                var list = collection as List<Instance>;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add when e.NewItems != null:
                        {
                            if (byIndex && insert != null)
                            {
                                if (e.NewItems.Count == 1 && e.NewItems[0] is Instance i)
                                {
                                    insert(e.NewStartingIndex, i);
                                }
                                else if (list != null)
                                {
                                    list.InsertRange(e.NewStartingIndex, e.NewItems.Cast<Instance>());
                                }
                                else
                                {
                                    int k = e.NewStartingIndex;
                                    foreach (var j in e.NewItems.Cast<Instance>())
                                    {
                                        insert(k++, j);
                                    }
                                }
                            }
                            else
                            {
                                foreach (var i in e.NewItems.Cast<Instance>())
                                {
                                    collection.Add(i);
                                }
                            }
                            break;
                        }
                    case NotifyCollectionChangedAction.Remove when e.OldItems != null:
                        {
                            if (byIndex && remove != null)
                            {
                                if (e.OldItems.Count == 1)
                                {
                                    remove(e.OldStartingIndex);
                                }
                                else if (list != null)
                                {
                                    list.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                                }
                                else
                                {
                                    for (int k = e.OldStartingIndex + e.OldItems.Count - 1; k >= e.OldStartingIndex; --k)
                                    {
                                        remove(k);
                                    }
                                }
                            }
                            else
                            {
                                foreach (var i in e.OldItems.Cast<Instance>())
                                {
                                    collection.Remove(i);
                                }
                            }

                            break;
                        }
                    case NotifyCollectionChangedAction.Replace:
                        if (byIndex && e.NewItems != null && set != null)
                        {
                            set(e.NewStartingIndex, e.NewItems[0]);
                        }
                        else
                        {
                            if (e.OldItems != null)
                            {
                                foreach (var i in e.OldItems.Cast<Instance>())
                                {
                                    collection.Remove(i);
                                }
                            }
                            if (e.NewItems != null)
                            {
                                foreach (var i in e.NewItems.Cast<Instance>())
                                {
                                    collection.Add(i);
                                }
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        collection.Clear();
                        if (list != null)
                        {
                            list.AddRange(container);
                        }
                        else
                        {
                            foreach (var i in container)
                            {
                                collection.Add(i);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        if (byIndex && get != null && insert != null && remove != null)
                        {
                            var i = get(e.OldStartingIndex);
                            remove(e.OldStartingIndex);
                            insert(e.NewStartingIndex, i);
                        }
                        break;
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
