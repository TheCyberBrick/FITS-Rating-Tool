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

using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.Exporters;
using System;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.UI.Evaluation
{
    public interface IEvaluationExporterConfiguratorViewModel
    {
        public record Of();

        public class ExporterConfiguratorFactory
        {
            public string Id { get; }

            public string Name { get; }

            public IDelegatedFactory<IExporterConfiguratorViewModel> Factory { get; }

            public ExporterConfiguratorFactory(string id, string name, IDelegatedFactory<IExporterConfiguratorViewModel> factory)
            {
                Id = id;
                Name = name;
                Factory = factory;
            }
        }

        IReadOnlyList<ExporterConfiguratorFactory> ExporterConfiguratorFactories { get; }

        ExporterConfiguratorFactory? SelectedExporterConfiguratorFactory { get; set; }

        IExporterConfiguratorViewModel? ExporterConfigurator { get; }

        void SetExporterConfigurator(IDelegatedFactory<IExporterConfiguratorViewModel>? factory);


        event EventHandler ConfigurationChanged;
    }
}
