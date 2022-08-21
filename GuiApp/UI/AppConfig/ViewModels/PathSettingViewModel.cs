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
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.AppConfig.ViewModels
{
    public enum PathType
    {
        File,
        Directory,
        Any
    }

    public interface IPathProperties
    {
        string Name { get; }

        PathType PathType { get; }

        bool AllowFilePath { get; }

        bool AllowDirectoryPath { get; }

        IReadOnlyList<string> AllowedFileExtensions { get; }

        bool AllowAnyFileExtension { get; }

        bool IsPathAllowed(string path);
    }

    public class PathSettingViewModel : SettingViewModel, IPathProperties
    {
        public override IConfigSetting Setting { get; }

        public PathType PathType { get; }

        public bool AllowFilePath => PathType == PathType.File || PathType == PathType.Any;

        public bool AllowDirectoryPath => PathType == PathType.Directory || PathType == PathType.Any;

        public IReadOnlyList<string> AllowedFileExtensions { get; }

        public bool AllowAnyFileExtension => AllowedFileExtensions.Count == 0;

        public ReactiveCommand<Unit, Unit> SelectPathWithOpenDialog { get; }

        public Interaction<IPathProperties, string> SelectPathOpenDialog { get; } = new();

        public PathSettingViewModel(string name, Func<string> getter, Action<string> setter, PathType pathType, List<string>? allowedFileExtensions = null) : base(name)
        {
            Setting = new ConfigSetting<string>(getter, setter);

            PathType = pathType;

            if (allowedFileExtensions != null)
            {
                AllowedFileExtensions = new List<string>(allowedFileExtensions);
            }
            else
            {
                AllowedFileExtensions = new List<string>();
            }

            SelectPathWithOpenDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                Setting.Value = await SelectPathOpenDialog.Handle(this);
            });
        }

        public bool IsPathAllowed(string path)
        {
            return (AllowFilePath && File.Exists(path) && IsFileAllowed(path)) || (AllowDirectoryPath && Directory.Exists(path));
        }

        private bool IsFileAllowed(string file)
        {
            if (!AllowFilePath)
            {
                return false;
            }
            if (AllowAnyFileExtension)
            {
                return true;
            }
            var ext = Path.GetExtension(file);
            return ext != null && AllowedFileExtensions.Contains(ext);
        }
    }
}
