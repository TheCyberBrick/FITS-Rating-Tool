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

using System;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.Repositories
{
    public interface IFileRepository
    {
        public class FileEventArgs : EventArgs
        {
            public string File { get; private set; }

            public bool Added { get; private set; }

            public bool Removed { get => !Added; }

            public FileEventArgs(string file, bool added)
            {
                File = file;
                Added = added;
            }
        }

        IReadOnlyList<string> Files { get; }

        void AddFile(string file);

        void RemoveFile(string file);

        bool ContainsFile(string file);


        event EventHandler<FileEventArgs> FileRemoved;

        event EventHandler<FileEventArgs> FileAdded;
    }
}
