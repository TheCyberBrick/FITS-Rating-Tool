﻿/*
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

using FitsRatingTool.Common.Services;
using FitsRatingTool.GuiApp.Models;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class AppConfig : IAppConfig
    {
        private IAppConfigManager manager;
        private IGroupingManager groupingManager;

        public AppConfig(IAppConfigManager manager, IGroupingManager groupingManager)
        {
            this.manager = manager;
            this.groupingManager = groupingManager;
        }

        #region Viewer
        public int AutoLoadMaxImageCount
        {
            get => int.TryParse(manager.Get("AutoLoadMaxImageCount"), out int count) ? count : 64;
            set => manager.Set("AutoLoadMaxImageCount", value.ToString());
        }
        #endregion

        #region Evaluation
        public string DefaultEvaluationFormulaPath
        {
            get => manager.Get("DefaultEvaluationFormulaPath") ?? "";
            set => manager.Set("DefaultEvaluationFormulaPath", value);
        }

        public GroupingConfiguration DefaultEvaluationGrouping
        {
            get => StringToGrouping(manager.Get("DefaultEvaluationGrouping") ?? "", out var grouping) ? grouping! : new GroupingConfiguration(true, true, false, false, false, false, 0, null);
            set => manager.Set("DefaultEvaluationGrouping", GroupingToString(value));
        }
        #endregion

        #region Voyager Integration
        public bool VoyagerIntegrationEnabled
        {
            get => bool.TryParse(manager.Get("VoyagerIntegrationEnabled"), out bool enabled) ? enabled : false;
            set => manager.Set("VoyagerIntegrationEnabled", value.ToString());
        }

        public string VoyagerAddress
        {
            get => manager.Get("VoyagerAddress") ?? "127.0.0.1";
            set => manager.Set("VoyagerAddress", value);
        }

        public int VoyagerPort
        {
            get => int.TryParse(manager.Get("VoyagerPort"), out int port) ? port : 5950;
            set => manager.Set("VoyagerPort", value.ToString());
        }

        public string VoyagerUsername
        {
            get => manager.Get("VoyagerUsername") ?? "admin";
            set => manager.Set("VoyagerUsername", value);
        }

        public string VoyagerPassword
        {
            get => System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(manager.Get("VoyagerPassword") ?? "YWRtaW4="));
            set => manager.Set("VoyagerPassword", System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value)));
        }
        public string RoboTargetSecret
        {
            get => System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(manager.Get("RoboTargetSecret") ?? ""));
            set => manager.Set("RoboTargetSecret", System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value)));
        }
        #endregion

        private string GroupingToString(GroupingConfiguration? grouping)
        {
            if (grouping == null)
            {
                return string.Empty;
            }
            return string.Join(',', grouping.GroupingKeys);
        }

        private bool StringToGrouping(string value, out GroupingConfiguration? grouping)
        {
            if (value.Trim().Length == 0)
            {
                grouping = new GroupingConfiguration(false, false, false, false, false, false, 0, null);
                return true;
            }
            return GroupingConfiguration.TryParseGroupingKeys(groupingManager, value.Split(','), out grouping);
        }
    }
}