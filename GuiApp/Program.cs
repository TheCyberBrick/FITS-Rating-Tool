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

using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace FitsRatingTool.GuiApp
{
    internal class Program
    {
        private static string MUTEX_GUID = "3bfc03af-2ba0-422b-ab8d-affa158b9af3";
        private static string PIPE_GUID = "f1e0964f-010f-4f6f-8bcf-75d7401c79a9";

        private static EventHandler<string>? _onOpenFile;
        public static event EventHandler<string> OnOpenFile
        {
            add => _onOpenFile += value;
            remove => _onOpenFile -= value;
        }

        private static volatile bool _openFileInNewWindow = false;
        public static bool OpenFileInNewWindow
        {
            get => _openFileInNewWindow;
            set => _openFileInNewWindow = value;
        }

        public static string? LaunchFilePath
        {
            get;
            private set;
        }

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            var mutex = new Mutex(true, MUTEX_GUID, out bool isOwner);

            if (isOwner)
            {
                // Wait for new pipe connections and raise OnOpenFile event
                // when a file is received
                var serverThread = new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            using (var stream = new NamedPipeServerStream(PIPE_GUID, PipeDirection.InOut, 1))
                            {
                                stream.WaitForConnection();

                                if (OpenFileInNewWindow)
                                {
                                    stream.WriteByte(1);
                                }
                                else
                                {
                                    stream.WriteByte(0);

                                    using (var reader = new StreamReader(stream))
                                    {
                                        var file = reader.ReadLine();
                                        if (file != null)
                                        {
                                            _onOpenFile?.Invoke(null, file);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Failed receiving file path:");
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);

                            Thread.Sleep(500);
                        }
                    }
                });
                serverThread.Name = "FileOpenThread";
                serverThread.IsBackground = true;
                serverThread.Start();
            }

            if (args.Length >= 1)
            {
                LaunchFilePath = args[0];

                if (!isOwner)
                {
                    try
                    {
                        using (var stream = new NamedPipeClientStream(PIPE_GUID))
                        {
                            stream.Connect(1000);

                            if (stream.ReadByte() == 0)
                            {
                                // Delegate to already running instance
                                using (var writer = new StreamWriter(stream))
                                {
                                    writer.WriteLine(LaunchFilePath);
                                }

                                // And exit
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Failed sending file path:");
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                        return;
                    }
                }
            }

            // Launch Avalonia App
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .With(new Win32PlatformOptions
                {
                    AllowEglInitialization = false
                })
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }
}
