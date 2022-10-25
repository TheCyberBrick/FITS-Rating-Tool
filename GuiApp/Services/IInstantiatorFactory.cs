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

namespace FitsRatingTool.GuiApp.Services
{
    public interface IInstantiatorFactory<T, Template> : IDisposable
        where T : class
    {
        ITemplatedInstantiator<T, Template> Templated(Func<Template?> templateConstructor);

        IDelegatedInstantiator<T, Template> Delegated(Func<Template?> templateConstructor, Func<Template, T?> instanceConstructor, Action<T> instanceDestructor);
    }

    public interface IInstantiatorBase<T>
    {
        bool IsExpired { get; }

        IInstantiatorBase<T> AndThen(Action<T> action);
    }

    public interface IGenericInstantiator<T, Template> : IInstantiatorBase<T>
    {
        new IGenericInstantiator<T, Template> AndThen(Action<T> action);

        T Instantiate(Func<Template, T> instanceConstructor, Action<T> instanceDestructor, out IDisposable disposable);
    }

    public interface ITemplatedInstantiator<T, Template> : IGenericInstantiator<T, Template>
        where T : class
    {
        new ITemplatedInstantiator<T, Template> AndThen(Action<T> action);

        T Instantiate(Func<Template, T> instanceConstructor);
    }

    public interface IDelegatedInstantiator<T> : IInstantiatorBase<T>
        where T : class
    {
        new IDelegatedInstantiator<T> AndThen(Action<T> action);

        T Instantiate(out IDisposable disposable);
    }

    public interface IDelegatedInstantiator<T, Template> : IGenericInstantiator<T, Template>, IDelegatedInstantiator<T>
        where T : class
    {
        new IDelegatedInstantiator<T, Template> AndThen(Action<T> action);
    }
}
