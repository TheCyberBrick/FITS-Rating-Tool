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

using Avalonia.Utilities;
using FitsRatingTool.GuiApp.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.UI
{
    public class ViewModelBase : ReactiveObject, IActivatableViewModel, IContainerLifecycleListener, IContainerDependencyListener
    {
        public ViewModelActivator Activator { get; }

        private readonly List<Action> cleanup = new();
        private readonly Dictionary<(object, string, object), Action> subscribedEvents = new();

        public ViewModelBase()
        {
            Activator = new ViewModelActivator();
        }

        protected void WhenDestroying(Action action)
        {
            cleanup.Add(action);
        }

        protected void SubscribeToEvent<TTarget, TEventArgs, TSubscriber>(TTarget target, string eventName, EventHandler<TEventArgs> subscriber)
            where TEventArgs : EventArgs
            where TSubscriber : class
            where TTarget : class
        {
            if (subscribedEvents.TryAdd((target, eventName, subscriber), () => WeakEventHandlerManager.Unsubscribe<TEventArgs, TSubscriber>(target, eventName, subscriber)))
            {
                WeakEventHandlerManager.Subscribe<TTarget, TEventArgs, TSubscriber>(target, eventName, subscriber);
            }
        }

        protected void UnsubscribeFromEvent<TTarget, TEventArgs, TSubscriber>(TTarget target, string eventName, EventHandler<TEventArgs> subscriber)
            where TEventArgs : EventArgs
            where TSubscriber : class
            where TTarget : class
        {
            if (subscribedEvents.Remove((target, eventName, subscriber), out var action))
            {
                action.Invoke();
            }
        }

        void IContainerLifecycleListener.OnInstantiated()
        {
            OnInstantiated();
        }

        protected virtual void OnInstantiated()
        {
        }

        void IContainerLifecycleListener.OnDestroying()
        {
            foreach (var action in subscribedEvents.Values)
            {
                action.Invoke();
            }
            subscribedEvents.Clear();

            foreach (var action in cleanup)
            {
                action.Invoke();
            }
            cleanup.Clear();

            OnDestroying();
        }

        protected virtual void OnDestroying()
        {
        }

        void IContainerLifecycleListener.OnDestroyed()
        {
            OnDestroyed();
        }

        protected virtual void OnDestroyed()
        {
        }

        void IContainerDependencyListener.OnAdded(object dependency)
        {
            OnAdded(dependency);
        }

        protected virtual void OnAdded(object dependency)
        {
        }

        void IContainerDependencyListener.OnRemoved(object dependency)
        {
            OnRemoved(dependency);
        }

        protected virtual void OnRemoved(object dependency)
        {
        }

        void IContainerDependencyListener.OnAddedTo(object dependee)
        {
            OnAddedTo(dependee);
        }

        protected virtual void OnAddedTo(object dependee)
        {
        }
    }
}
