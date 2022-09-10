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

namespace FitsRatingTool.Common.Models.Evaluation
{
    public interface IEvaluationExporterEventDispatcher
    {
        public class ExporterEventArgs : EventArgs
        {
            public string Name { get; }

            public object? Parameter { get; }

            public ExporterEventArgs(string name, object? parameter)
            {
                Name = name;
                Parameter = parameter;
            }
        }

        void Send(IEvaluationExporter sender, string name, object? parameter);
    }
}
