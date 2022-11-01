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
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    [Export(typeof(IExporterConfiguratorManager)), SingletonReuse]
    public class ExporterConfiguratorManager : IExporterConfiguratorManager
    {
        private readonly Dictionary<string, IExporterConfiguratorManager.FactoryInfo> factories = new();

        public IEnumerable<KeyValuePair<string, IExporterConfiguratorManager.FactoryInfo>> Factories => factories;

        public bool Register(string id, IExporterConfiguratorManager.FactoryInfo factory)
        {
            return factories.TryAdd(id, factory);
        }

        public IExporterConfiguratorManager.FactoryInfo? Get(string id)
        {
            factories.TryGetValue(id, out var factory);
            return factory;
        }
    }
}
