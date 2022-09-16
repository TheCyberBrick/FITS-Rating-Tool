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

using ReactiveUI;
using System.Reactive;
using FitsRatingTool.GuiApp.Services;

namespace FitsRatingTool.GuiApp.UI.Exporters
{
    public interface IVoyagerExporterConfiguratorViewModel : IExporterConfiguratorManager.IExporterConfiguratorViewModel
    {
        public interface IFactory
        {
            public IVoyagerExporterConfiguratorViewModel Create();
        }

        string ApplicationServerHostname { get; set; }

        int ApplicationServerPort { get; set; }

        string CredentialsFile { get; set; }

        ReactiveCommand<Unit, Unit> SelectCredentialsFileWithOpenFileDialog { get; }

        ReactiveCommand<Unit, Unit> CreateCredentialsFileWithSaveFileDialog { get; }

        Interaction<string, string> SelectCredentialsFileOpenFileDialog { get; }

        Interaction<string, string> CreateCredentialsFileSaveFileDialog { get; }

        bool IsMinRatingThresholdEnabled { get; set; }

        int MinRatingThreshold { get; set; }

        bool IsMaxRatingThresholdEnabled { get; set; }

        int MaxRatingThreshold { get; set; }
    }
}
