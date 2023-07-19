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
using FitsRatingTool.GuiApp.UI.Utils.ViewModels;
using FitsRatingTool.IoC;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.UI.Variables.ViewModels
{
    [Export(typeof(IVariableEditorViewModel)), TransientReuse, AllowDisposableTransient]
    public class VariableEditorViewModel : RegistryItemEditorViewModel<IVariableConfiguratorViewModel>, IVariableEditorViewModel
    {
        public VariableEditorViewModel(IRegistrar<IVariableEditorViewModel, IVariableEditorViewModel.Of> reg)
        {
            reg.RegisterAndReturn<VariableEditorViewModel>();
        }

        private VariableEditorViewModel(IVariableEditorViewModel.Of args,
            IContainer<IVariableSelectorViewModel, IVariableSelectorViewModel.Of> variableSelectorContainer)
        {
            variableSelectorContainer.Singleton().Inject(new(), SetSelector);
        }
    }
}
