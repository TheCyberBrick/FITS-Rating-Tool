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
using System;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IVariableContext
    {
        public class VariableConfigsChangedEventArgs : EventArgs
        {
            public IReadOnlyList<IReadOnlyJobConfig.VariableConfig>? OldConfigs { get; }

            public IReadOnlyList<IReadOnlyJobConfig.VariableConfig>? NewConfigs { get; }

            public VariableConfigsChangedEventArgs(IReadOnlyList<IReadOnlyJobConfig.VariableConfig>? oldConfigs, IReadOnlyList<IReadOnlyJobConfig.VariableConfig>? newConfigs)
            {
                OldConfigs = oldConfigs;
                NewConfigs = newConfigs;
            }
        }

        public class VariablesChangedEventArgs : EventArgs
        {
            public IReadOnlyList<IReadOnlyVariable>? OldVariables { get; }

            public IReadOnlyList<IReadOnlyVariable>? NewVariables { get; }

            public VariablesChangedEventArgs(IReadOnlyList<IReadOnlyVariable>? oldVariables, IReadOnlyList<IReadOnlyVariable>? newVariables)
            {
                OldVariables = oldVariables;
                NewVariables = newVariables;
            }
        }


        IReadOnlyList<IReadOnlyJobConfig.VariableConfig>? CurrentVariableConfigs { get; set; }

        IReadOnlyList<IReadOnlyVariable>? CurrentVariables { get; }


        void LoadFromOther(IVariableContext ctx);

        void LoadFromCurrentProfile(IInstrumentProfileContext ctx);


        event EventHandler<VariableConfigsChangedEventArgs> CurrentVariableConfigsChanged;

        event EventHandler<VariablesChangedEventArgs> CurrentVariablesChanged;
    }
}
