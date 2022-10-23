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
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class InstantiatorFactory<T, Template> : IInstantiatorFactory<T, Template>
        where T : class
    {
        private class Instantiator : IInstantiator<T, Template>, IDisposable
        {
            public bool IsExpired { get; private set; }

            private readonly List<Action<T>> actions = new();

            private readonly Func<Template?> templateFactory;

            public Instantiator(Func<Template?> templateFactory)
            {
                this.templateFactory = templateFactory;
            }

            public IInstantiator<T, Template> AndThen(Action<T> action)
            {
                lock (this)
                {
                    CheckExpired();
                    actions.Add(action);
                }
                return this;
            }

            public T Instantiate(Func<Template, T> factory)
            {
                lock (this)
                {
                    CheckExpired();

                    var template = templateFactory.Invoke();

                    if (template == null)
                    {
                        Dispose();
                    }

                    CheckExpired();

                    var instance = factory.Invoke(template!);

                    foreach (var action in actions)
                    {
                        action.Invoke(instance);
                    }

                    Dispose();

                    return instance;
                }
            }

            private void CheckExpired()
            {
                if (IsExpired)
                {
                    throw new ObjectDisposedException(nameof(Instantiator));
                }
            }

            public void Dispose()
            {
                lock (this)
                {
                    IsExpired = true;
                }
            }
        }

        private bool disposed;

        private readonly List<WeakReference<Instantiator>> instantiators = new();

        public IInstantiator<T, Template> Create(Func<Template?> templateFactory)
        {
            var instantiator = new Instantiator(templateFactory);

            lock (instantiators)
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(InstantiatorFactory<T, Template>));
                }

                Maintain();

                instantiators.Add(new(instantiator));
            }

            return instantiator;
        }

        public IInstantiator<T, Template> Create(Template template)
        {
            return Create(() => template);
        }

        private void Maintain()
        {
            lock (instantiators)
            {
                for (int i = instantiators.Count - 1; i >= 0; --i)
                {
                    if (!instantiators[i].TryGetTarget(out var _))
                    {
                        instantiators.RemoveAt(i);
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (instantiators)
            {
                disposed = true;

                foreach (var wref in instantiators)
                {
                    if (wref.TryGetTarget(out var instantiator))
                    {
                        instantiator.Dispose();
                    }
                }

                instantiators.Clear();
            }
        }
    }
}
