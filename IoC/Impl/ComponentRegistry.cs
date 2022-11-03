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

using System.Collections.Concurrent;
using System.Reactive.Disposables;

namespace FitsRatingTool.IoC.Impl
{
    public class ComponentRegistry<T> : IComponentRegistry<T>, IDisposable
        where T : class
    {
        public ComponentRegistry(IRegistrar<IComponentRegistry<T>, IComponentRegistry<T>.Of> reg)
        {
            reg.RegisterAndReturn<ComponentRegistry<T>>();
        }

        public ComponentRegistry(IRegistrar<IComponentRegistry<T>, IComponentRegistry<T>.OfRegistrations> reg)
        {
            reg.RegisterAndReturn<ComponentRegistry<T>>();
        }


        public IEnumerable<string> Ids => registrations.Keys;


        private readonly ConcurrentDictionary<string, (IComponentRegistration<T> Registration, IDelegatedFactory<T> Factory)> registrations = new();
        private readonly ConcurrentDictionary<string, IDisposable> disposables = new();

        private ComponentRegistry(IComponentRegistry<T>.Of args, IEnumerable<IComponentRegistration<T>> defaultRegistrations) : this(defaultRegistrations)
        {
        }

        private ComponentRegistry(IComponentRegistry<T>.OfRegistrations args, IEnumerable<IComponentRegistration<T>> defaultRegistrations) : this(defaultRegistrations, args.Registrations)
        {
        }

        private ComponentRegistry(IEnumerable<IComponentRegistration<T>> defaultRegistrations, params IComponentRegistration<T>[] additionalRegistrations)
        {
            void registerAndCheck(IComponentRegistration<T> registration)
            {
                if (!Register(registration))
                {
                    throw new IComponentRegistry<T>.RegistrationFailedException(registration.Id, $"Failed registering registration with ID '{registration.Id}'");
                }
            }

            foreach (var registration in defaultRegistrations)
            {
                registerAndCheck(registration);
            }

            foreach (var registration in additionalRegistrations)
            {
                registerAndCheck(registration);
            }
        }

        public IComponentRegistration<T>? GetRegistration(string id)
        {
            return registrations.TryGetValue(id, out var registration) ? registration.Registration : null;
        }

        public IDelegatedFactory<T>? GetFactory(string id)
        {
            return registrations.TryGetValue(id, out var registration) ? registration.Factory : null;
        }

        public bool Register(IComponentRegistration<T> registration)
        {
            var result = registrations.GetOrAdd(registration.Id, id =>
            {
                var disposable = new CompositeDisposable();

                var factory = registration.CreateFactory(disposable);

                if (!disposables.TryAdd(id, disposable))
                {
                    throw new InvalidOperationException("Failed registering factory disposable");
                }

                return (registration, factory);
            });

            return result.Registration == registration;
        }

        public void Dispose()
        {
            while (!registrations.IsEmpty)
            {
                foreach (var key in registrations.Keys)
                {
                    try
                    {
                        if (disposables.TryRemove(key, out var disposable))
                        {
                            disposable.Dispose();
                        }
                    }
                    finally
                    {
                        registrations.TryRemove(key, out var _);
                    }
                }
            }
        }
    }
}
