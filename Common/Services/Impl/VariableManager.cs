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

using FitsRatingTool.Common.Models.Evaluation;
using System.Diagnostics.CodeAnalysis;

namespace FitsRatingTool.Common.Services.Impl
{
    public class VariableManager : IVariableManager
    {
        private readonly Dictionary<string, IVariableManager.VariableFactory> variableFactories = new();

        public IReadOnlyDictionary<string, IVariableManager.VariableFactory> Variables => variableFactories;

        public bool Register(string id, IVariableManager.VariableFactory variableFactory)
        {
            return variableFactories.TryAdd(id, variableFactory);
        }

        public bool Unregister(string id)
        {
            return variableFactories.Remove(id);
        }

        public bool TryCreateVariable(string id, string name, string config, [NotNullWhen(true)] out IVariable? variable)
        {
            variable = null;

            bool isNameValid = name.Length > 0 && char.IsLetter(name[0]) && name.All(x => char.IsLetterOrDigit(x));
            if (!isNameValid)
            {
                return false;
            }

            if (variableFactories.TryGetValue(id, out var variableFactory))
            {
                variable = variableFactory.Invoke(name, config);
                return true;
            }

            return false;
        }
    }
}
