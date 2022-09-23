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

namespace FitsRatingTool.GuiApp.Repositories.Impl
{
    public class FileRepository : IFileRepository
    {
        private readonly HashSet<string> fileSet = new();
        private readonly List<string> files = new();

        public IReadOnlyList<string> Files => files;

        public void AddFile(string file)
        {
            if (fileSet.Add(file))
            {
                files.Add(file);
                _fileAdded?.Invoke(this, new IFileRepository.FileEventArgs(file, true));
            }
        }

        public void RemoveFile(string file)
        {
            if (fileSet.Remove(file))
            {
                files.Remove(file);
                _fileRemoved?.Invoke(this, new IFileRepository.FileEventArgs(file, false));
            }
        }

        public bool ContainsFile(string file)
        {
            return fileSet.Contains(file);
        }


        private event EventHandler<IFileRepository.FileEventArgs>? _fileRemoved;
        public event EventHandler<IFileRepository.FileEventArgs>? FileRemoved
        {
            add
            {
                _fileRemoved += value;
            }
            remove
            {
                _fileRemoved -= value;
            }
        }

        private event EventHandler<IFileRepository.FileEventArgs>? _fileAdded;
        public event EventHandler<IFileRepository.FileEventArgs>? FileAdded
        {
            add
            {
                _fileAdded += value;
            }
            remove
            {
                _fileAdded -= value;
            }
        }
    }
}
