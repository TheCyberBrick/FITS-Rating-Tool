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
    public interface IExporterConfiguratorManager
    {
        public interface IExporterConfiguratorViewModel
        {
            bool IsValid { get; }

            string CreateConfig();

            IEvaluationExporter CreateExporter(IEvaluationExporterContext ctx);

            event EventHandler ConfigurationChanged;

            bool TryLoadConfig(string config);
        }

        public class Factory
        {
            public virtual string Name { get; }

            private readonly Func<IExporterConfiguratorViewModel> factory;

            public Factory(string name, Func<IExporterConfiguratorViewModel> factory)
            {
                Name = name;
                this.factory = factory;
            }

            public virtual IExporterConfiguratorViewModel CreateConfigurator()
            {
                return factory.Invoke();
            }
        }

        IEnumerable<KeyValuePair<string, Factory>> Factories { get; }

        bool Register(string id, Factory factory);

        Factory? Get(string id);
    }
}
