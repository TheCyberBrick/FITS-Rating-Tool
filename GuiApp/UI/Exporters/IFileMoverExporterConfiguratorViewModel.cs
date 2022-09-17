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

namespace FitsRatingTool.GuiApp.UI.Exporters
{
    public interface IFileMoverExporterConfiguratorViewModel : IExporterConfiguratorManager.IExporterConfiguratorViewModel
    {
        public interface IFactory
        {
            public IFileMoverExporterConfiguratorViewModel Create();
        }

        bool IsMinRatingThresholdEnabled { get; set; }

        float MinRatingThreshold { get; set; }

        bool IsMaxRatingThresholdEnabled { get; set; }

        float MaxRatingThreshold { get; set; }

        bool IsLessThanRule { get; }

        bool IsGreaterThanRule { get; }

        bool IsLessThanOrGreaterThanRule { get; }

        bool IsRelativePath { get; set; }

        int ParentDirs { get; set; }
    }
}
