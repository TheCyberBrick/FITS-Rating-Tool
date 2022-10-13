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

using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.FitsImage
{
    public interface IFitsImageMultiViewerViewModel : IDisposable
    {
        public interface IFactory
        {
            IFitsImageMultiViewerViewModel Create();
        }

        public class ViewerEventArgs : EventArgs
        {
            public IFitsImageViewerViewModel Viewer { get; }

            public bool Unloaded => _unloaded;

            public bool Loaded => !_unloaded;


            private readonly bool _unloaded;


            public ViewerEventArgs(IFitsImageViewerViewModel viewer, bool unloaded)
            {
                Viewer = viewer;
                _unloaded = unloaded;
            }
        }

        public class Instance
        {
            public IFitsImageViewerViewModel Viewer { get; }

            public ReactiveCommand<Unit, Unit> Close { get; }

            public bool IsCloseable { get; }

            public Instance(IFitsImageViewerViewModel viewer, bool isCloseable)
            {
                Viewer = viewer;
                IsCloseable = isCloseable;
                Close = ReactiveCommand.Create(() => { }, Observable.Return(isCloseable));
            }
        }

        IFitsImageViewerViewModel.IOverlayFactory? InnerOverlayFactory { get; set; }

        IFitsImageViewerViewModel.IOverlayFactory? OuterOverlayFactory { get; set; }

        int MaxViewers { get; set; }

        ObservableCollection<Instance> Instances { get; }

        Instance? SelectedInstance { get; set; }

        string? File { get; set; }

        // TODO FileName getter

        IFitsImageViewModel? FitsImage { get; set; }

        event EventHandler<ViewerEventArgs> ViewerLoaded;

        event EventHandler<ViewerEventArgs> ViewerUnloaded;
    }
}
