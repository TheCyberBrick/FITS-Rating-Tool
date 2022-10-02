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

using FitsRatingTool.GuiApp.Models;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IAppConfig
    {
        #region Misc
        bool OpenFileInNewWindow { get; set; }

        string DefaultInstrumentProfileId { get; set; }

        bool InstrumentProfileChangeConfirmation { get; set; }
        #endregion

        #region Viewer
        bool KeepImageDataLoaded { get; set; }

        int AutoLoadMaxImageCount { get; set; }

        long MaxImageSize { get; set; }

        int MaxImageWidth { get; set; }

        int MaxImageHeight { get; set; }

        int MaxThumbnailWidth { get; set; }

        int MaxThumbnailHeight { get; set; }
        #endregion

        #region Evaluation
        string DefaultEvaluationFormulaPath { get; set; }

        GroupingConfiguration DefaultEvaluationGrouping { get; set; }
        #endregion

        #region Voyager Integration
        bool VoyagerIntegrationEnabled { get; set; }

        string VoyagerAddress { get; set; }

        int VoyagerPort { get; set; }

        string VoyagerUsername { get; set; }

        string VoyagerPassword { get; set; }

        string RoboTargetSecret { get; set; }
        #endregion
    }
}
