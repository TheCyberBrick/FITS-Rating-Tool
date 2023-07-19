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

using FitsRatingTool.IoC;
using ReactiveUI;
using System;
using System.ComponentModel.Composition;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.Utils.ViewModels
{
    public class RegistryItemEditorViewModel<TConfigurator> : ViewModelBase, IItemEditorViewModel<TConfigurator>, IDisposable
        where TConfigurator : class, IItemConfigurator
    {
        private readonly ObservableAsPropertyHelper<bool> _isValid;
        public bool IsValid => _isValid.Value;

        public IItemSelectorViewModel Selector { get; private set; } = null!;

        private IDisposable? _configuratorDisposable;
        private TConfigurator? _configurator;
        public TConfigurator? Configurator
        {
            get => _configurator;
            private set => this.RaiseAndSetIfChanged(ref _configurator, value);
        }

        private EventHandler? _configurationChanged;
        public event EventHandler ConfigurationChanged
        {
            add => _configurationChanged += value;
            remove => _configurationChanged -= value;
        }


        [Import]
        protected IContainer<IComponentRegistry<TConfigurator>, IComponentRegistry<TConfigurator>.Of> RegistryContainer { get; private set; } = null!;

        protected IComponentRegistry<TConfigurator> Registry { get; private set; } = null!;


        public RegistryItemEditorViewModel()
        {
            _isValid = this.WhenAnyValue(x => x.Configurator!.IsValid).ToProperty(this, x => x.IsValid);
        }

        protected void SetSelector(IItemSelectorViewModel selector)
        {
            if (Selector != null)
            {
                throw new InvalidOperationException("Selector was already set");
            }

            Selector = selector;

            this.WhenAnyValue(x => x.Selector.SelectedItem)
                .Skip(1)
                .Subscribe(x => ReplaceConfigurator(x?.Id));
        }

        protected override void OnInstantiated()
        {
            if (Selector == null)
            {
                throw new InvalidOperationException("Selector was not set");
            }
            Reset();
        }

        public void Reset()
        {
            Selector.Reset();

            Registry = null!;
            RegistryContainer.Destroy();

            Registry = RegistryContainer.Instantiate(new());
        }

        private void ReplaceConfigurator(string? id)
        {
            ReplaceConfigurator(id == null ? null : Registry.GetFactory(id));
        }

        private void ReplaceConfigurator(IDelegatedFactory<TConfigurator>? factory)
        {
            IDisposable? newConfiguratorDisposable = null;
            TConfigurator? newConfigurator = factory != null ? factory.Instantiate(out newConfiguratorDisposable) : null;

            var oldConfigurator = Configurator;

            if (oldConfigurator != null)
            {
                if (newConfigurator != null && oldConfigurator.GetType() == newConfigurator.GetType())
                {
                    // Same configurator, no changes needed
                    newConfiguratorDisposable?.Dispose();
                    return;
                }

                UnsubscribeFromEvent<TConfigurator, EventArgs, RegistryItemEditorViewModel<TConfigurator>>(oldConfigurator, nameof(oldConfigurator.ConfigurationChanged), OnConfigurationChanged);
            }

            var oldConfiguratorDisposable = _configuratorDisposable;

            _configuratorDisposable = newConfiguratorDisposable;
            Configurator = newConfigurator;

            oldConfiguratorDisposable?.Dispose();

            if (newConfigurator != null)
            {
                SubscribeToEvent<TConfigurator, EventArgs, RegistryItemEditorViewModel<TConfigurator>>(newConfigurator, nameof(newConfigurator.ConfigurationChanged), OnConfigurationChanged);
            }

            _configurationChanged?.Invoke(this, new EventArgs());
        }

        private void OnConfigurationChanged(object? sender, EventArgs e)
        {
            if (sender == Configurator)
            {
                _configurationChanged?.Invoke(this, new EventArgs());
            }
        }

        public void SetConfigurator(IDelegatedFactory<TConfigurator>? delegatedFactory)
        {
            ReplaceConfigurator(delegatedFactory); // Set configurator first so it keeps its loaded data

            if (delegatedFactory != null)
            {
                foreach (var id in Registry.Ids)
                {
                    var registration = Registry.GetRegistration(id);
                    var factory = Registry.GetFactory(id);

                    if (registration != null && factory != null && factory.InstanceType == delegatedFactory.InstanceType)
                    {
                        if (Selector.SelectById(registration.Id) != null)
                        {
                            return;
                        }
                    }
                }
                throw new InvalidOperationException("Tried setting " + typeof(TConfigurator).FullName + " configurator to unknown type '" + delegatedFactory.InstanceType.FullName + "'");
            }
            else
            {
                Selector.SelectedItem = null;
            }
        }

        public virtual void Dispose()
        {
            _configuratorDisposable?.Dispose();
            _configuratorDisposable = null;
        }
    }
}
