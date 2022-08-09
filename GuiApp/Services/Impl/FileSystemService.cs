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
using System.Diagnostics;
using System.IO;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class FileSystemService : IFileSystemService
    {
        public bool ShowDirectory(string dir)
        {
            dir = dir.Replace("/", "\\");
            if (!Directory.Exists(dir))
            {
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = dir,
                    UseShellExecute = true,
                    Verb = "open"
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ShowFile(string file)
        {
            file = file.Replace("/", "\\");
            if (!File.Exists(file))
            {
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "explorer.exe",
                    Arguments = "/select, \"" + file + "\""
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
