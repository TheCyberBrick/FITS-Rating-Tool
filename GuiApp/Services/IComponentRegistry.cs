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
using System.Reactive.Disposables;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IComponentRegistry<T>
        where T : class
    {
        public class RegistrationFailedException : Exception
        {
            public string Id { get; }

            public RegistrationFailedException(string id, string message) : base(message)
            {
                Id = id;
            }
        }

        public record Of();

        public record OfRegistrations(params IComponentRegistration<T>[] Registrations);

        Type BaseType => typeof(T);

        IEnumerable<string> Ids { get; }

        IComponentRegistration<T>? GetRegistration(string id);

        IDelegatedFactory<T>? GetFactory(string id);

        bool Register(IComponentRegistration<T> registration);
    }

    public interface IComponentRegistration<T>
        where T : class
    {
        string Id { get; }

        string Name { get; }

        Type BaseType => typeof(T);

        IDelegatedFactory<T> CreateFactory(CompositeDisposable disposable);
    }
}
