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

using DryIocAttributes;
using FitsRatingTool.IoC;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.UI.Variables.ViewModels
{
    [Export(typeof(IVariableSelectorViewModel)), TransientReuse]
    public class VariableSelectorViewModel : ViewModelBase, IVariableSelectorViewModel
    {
        public VariableSelectorViewModel(IRegistrar<IVariableSelectorViewModel, IVariableSelectorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<VariableSelectorViewModel>();
        }

        public ReadOnlyObservableCollection<IVariableSelectorViewModel.VariableChoice> Variables { get; }

        private IVariableSelectorViewModel.VariableChoice? _selectedVariable;
        public IVariableSelectorViewModel.VariableChoice? SelectedVariable
        {
            get => _selectedVariable;
            set => this.RaiseAndSetIfChanged(ref _selectedVariable, value);
        }


        private ObservableCollection<IVariableSelectorViewModel.VariableChoice> _variables = new();


        private VariableSelectorViewModel(IVariableSelectorViewModel.Of args,
            IContainer<IComponentRegistry<IVariableConfiguratorViewModel>, IComponentRegistry<IVariableConfiguratorViewModel>.Of> variableConfiguratorRegistryContainer)
        {
            Variables = new ReadOnlyObservableCollection<IVariableSelectorViewModel.VariableChoice>(_variables);

            variableConfiguratorRegistryContainer.Singleton().Inject(new IComponentRegistry<IVariableConfiguratorViewModel>.Of(), registry =>
            {
                foreach (var id in registry.Ids)
                {
                    var registration = registry.GetRegistration(id);
                    var factory = registry.GetFactory(id);

                    if (registration != null && factory != null)
                    {
                        _variables.Add(new IVariableSelectorViewModel.VariableChoice(registration.Id, registration.Name));
                    }
                }
            });
        }

        public IVariableSelectorViewModel.VariableChoice? FindById(string id)
        {
            foreach (var choice in _variables)
            {
                if (choice.Id == id)
                {
                    return choice;
                }
            }
            return null;
        }

        public IVariableSelectorViewModel.VariableChoice? SelectById(string id)
        {
            var choice = FindById(id);
            if (choice != null)
            {
                SelectedVariable = choice;
            }
            return choice;
        }

    }
}
