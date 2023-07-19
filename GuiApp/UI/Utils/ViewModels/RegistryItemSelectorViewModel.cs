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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.UI.Utils.ViewModels
{
    public class RegistryItemSelectorViewModel<TConfigurator> : ViewModelBase, IItemSelectorViewModel
        where TConfigurator : class
    {
        public ReadOnlyObservableCollection<IItemSelectorViewModel.Item> Items { get; }

        private IItemSelectorViewModel.Item? _selectedItem;
        public IItemSelectorViewModel.Item? SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }


        private ObservableCollection<IItemSelectorViewModel.Item> _items = new();

        [Import]
        protected IContainer<IComponentRegistry<TConfigurator>, IComponentRegistry<TConfigurator>.Of> RegistryContainer { get; private set; } = null!;


        protected RegistryItemSelectorViewModel()
        {
            Items = new ReadOnlyObservableCollection<IItemSelectorViewModel.Item>(_items);
        }

        protected override void OnInstantiated()
        {
            Reset();
        }

        public void Reset()
        {
            SelectedItem = null;
            _items.Clear();

            RegistryContainer.Destroy();

            RegistryContainer.Inject(new(), registry =>
            {
                foreach (var id in registry.Ids)
                {
                    var registration = registry.GetRegistration(id);
                    var factory = registry.GetFactory(id);

                    if (registration != null && factory != null)
                    {
                        _items.Add(new IItemSelectorViewModel.Item(registration.Id, registration.Name));
                    }
                }
            });
        }

        public IItemSelectorViewModel.Item? FindById(string id)
        {
            foreach (var item in _items)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }
            return null;
        }

        public IItemSelectorViewModel.Item? SelectById(string id)
        {
            var item = FindById(id);
            if (item != null)
            {
                SelectedItem = item;
            }
            return item;
        }
    }
}
