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
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IVoyagerIntegration
    {
        public class NewImageEventArgs : EventArgs
        {
            public string File { get; }

            public NewImageEventArgs(string file)
            {
                File = file;
            }
        }

        public class ConnectionChangedEventArgs : EventArgs
        {
            public bool Connected { get; }

            public bool Disconnected => !Connected;

            public ConnectionChangedEventArgs(bool connected)
            {
                Connected = connected;
            }
        }

        string ApplicationServerHostname { get; set; }

        int ApplicationServerPort { get; set; }

        string? ApplicationServerUsername { get; set; }

        string? ApplicationServerPassword { get; set; }

        string? RoboTargetSecret { get; set; }

        Task StartAsync();

        Task StopAsync();

        event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;

        event EventHandler<NewImageEventArgs> NewImage;
    }
}
