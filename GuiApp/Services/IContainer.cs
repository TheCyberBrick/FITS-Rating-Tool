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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IContainer<T, Template> : INotifyPropertyChanged, INotifyPropertyChanging
        where T : class
    {
        object? Owner { get; }

        bool IsInitialized { get; }

        IContainer<T, Template> Instantiate(Template template);

        void Clear();

        T Instance { get; }

        T? InstanceOrNull { get; }

        IObservable<T?> WhenChanged { get; }

        event Action<Template?> OnInitialized;
    }

    public interface IRegistrar<T, Template>
        where T : class
    {
        object ClassScope { get; }

        [DoesNotReturn]
        void RegisterAndReturn<TImpl>(object? scope = null, ConstructorInfo? constructor = null) where TImpl : class, T;
    }

    [AttributeUsage(AttributeTargets.Constructor)]
    public class InstantiatorAttribute : Attribute
    {
    }

    public interface IContainerEvents
    {
        void OnAdded(object dependency);

        void OnRemoved(object dependency);

        void OnAddedTo(object dependee);

        void OnInstantiated();
    }

    public interface IContainerLifecycle : IDisposable
    {
        object? Instance { get; }

        void Initialize(IContainerLifecycle? parent, object? owner);

        void Clear(bool dispose);
    }
}
