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

using Avalonia.Collections;
using ReactiveUI;
using System.Reactive;
using FitsRatingTool.GuiApp.Services;

namespace FitsRatingTool.GuiApp.UI.Exporters
{
    public interface IBaseExporterConfiguratorViewModel : IExporterConfiguratorManager.IExporterConfiguratorViewModel
    {
        bool UsesPath { get; }

        string Path { get; set; }

        public class FileExtension
        {
            public readonly string Name;
            public readonly string Extension;

            public FileExtension(string name, string extension)
            {
                Name = name;
                Extension = extension;
            }
        }

        ReactiveCommand<Unit, Unit> SelectPathWithSaveFileDialog { get; }

        Interaction<FileExtension, string> SelectPathSaveFileDialog { get; }



        bool UsesExportValue { get; }

        bool ExportValue { get; set; }



        bool UsesExportGroupKey { get; }

        bool ExportGroupKey { get; set; }



        bool UsesExportVariables { get; }

        bool ExportVariables { get; set; }

        public interface IExportVariablesFilterViewModel
        {
            ReactiveCommand<Unit, Unit> Remove { get; }

            string? Variable { get; set; }
        }

        bool IsExportVariablesFilterEnabled { get; set; }

        AvaloniaList<IExportVariablesFilterViewModel> ExportVariablesFilter { get; }

        ReactiveCommand<Unit, Unit> AddNewExportVariablesFilter { get; }

        void AddExportVariablesFilter(string? variable);

        void ClearExportVariablesFilters();
    }
}
