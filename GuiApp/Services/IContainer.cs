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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IContainer<T, Template> : IReadOnlyContainer<T>
        where T : class
    {
        IContainer<T, Template> ToSingleton();

        IObservable<T?> ToSingletonWithObservable();

        T Instantiate(Template template);

        bool Destroy(T instance);

        void Destroy();
    }

    public interface IReadOnlyContainer<T> : IReadOnlyCollection<T>, INotifyPropertyChanged, INotifyPropertyChanging, INotifyCollectionChanged
        where T : class
    {
        bool IsInitialized { get; }

        event Action OnInitialized;

        event Action<T> OnInstantiated;

        event Action<T> OnDestroyed;

        bool IsSingleton { get; }
    }

    public interface IRegistrar<T, Template>
        where T : class
    {
        object ClassScopeName { get; }

        [DoesNotReturn]
        void RegisterAndReturn<TImpl>(object? scopeName = null, ConstructorInfo? constructor = null) where TImpl : class, T;
    }

    [AttributeUsage(AttributeTargets.Constructor)]
    public class InstantiatorAttribute : Attribute
    {
    }

    public interface IContainerLifecycleListener
    {
        void OnInstantiated();

        void OnDestroying();

        void OnDestroyed();
    }

    public interface IContainerDependencyListener
    {
        void OnAdded(object dependency);

        void OnRemoved(object dependency);

        void OnAddedTo(object dependee);
    }

    public interface IContainerLifecycle
    {
        void Initialize(IContainerLifecycle? parent, object? dependee);

        void Destroy(bool dispose);
    }
}
