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

using Avalonia.Utilities;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.FitsImage;
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using ReactiveUI;
using System;
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    public class AppViewerOverlayViewModel : ViewModelBase, IAppViewerOverlayViewModel
    {
        public class Factory : IAppViewerOverlayViewModel.IFactory
        {
            private readonly IFitsImageCornerViewerViewModel.IFactory fitsImageCornerViewerFactory;

            public Factory(IFitsImageCornerViewerViewModel.IFactory fitsImageCornerViewerFactory)
            {
                this.fitsImageCornerViewerFactory = fitsImageCornerViewerFactory;
            }

            public IAppViewerOverlayViewModel Create(IFitsImageViewerViewModel viewer)
            {
                return new AppViewerOverlayViewModel(viewer, fitsImageCornerViewerFactory);
            }

            IFitsImageViewerViewModel.IOverlay IFitsImageViewerViewModel.IOverlayFactory.Create(IFitsImageViewerViewModel viewer)
            {
                return Create(viewer);
            }
        }


        public IFitsImageViewerViewModel Viewer { get; }


        private bool _isExternalViewerEnabled = true;
        public bool IsExternalViewerEnabled
        {
            get => _isExternalViewerEnabled;
            set => this.RaiseAndSetIfChanged(ref _isExternalViewerEnabled, value);
        }

        private bool _isExternalCornerViewerEnabled = true;
        public bool IsExternalCornerViewerEnabled
        {
            get => _isExternalCornerViewerEnabled;
            set => this.RaiseAndSetIfChanged(ref _isExternalCornerViewerEnabled, value);
        }

        private bool _isCornerViewerEnabled;
        public bool IsCornerViewerEnabled
        {
            get => _isCornerViewerEnabled;
            set => this.RaiseAndSetIfChanged(ref _isCornerViewerEnabled, value);
        }

        private double _cornerViewerPercentage = 0.125;
        public double CornerViewerPercentage
        {
            get => _cornerViewerPercentage;
            set => this.RaiseAndSetIfChanged(ref _cornerViewerPercentage, value);
        }

        private IFitsImageCornerViewerViewModel? _cornerViewer;
        public IFitsImageCornerViewerViewModel? CornerViewer
        {
            get => _cornerViewer;
            private set => this.RaiseAndSetIfChanged(ref _cornerViewer, value);
        }

        public ReactiveCommand<Unit, IFitsImageViewerViewModel> ShowExternalViewer { get; }

        public ReactiveCommand<Unit, IFitsImageCornerViewerViewModel> ShowExternalCornerViewer { get; }



        private readonly IFitsImageCornerViewerViewModel.IFactory fitsImageCornerViewerFactory;

        public AppViewerOverlayViewModel(IFitsImageViewerViewModel viewer, IFitsImageCornerViewerViewModel.IFactory fitsImageCornerViewerFactory)
        {
            this.fitsImageCornerViewerFactory = fitsImageCornerViewerFactory;

            Viewer = viewer;

            this.WhenAnyValue(x => x.IsCornerViewerEnabled).Subscribe(x =>
            {
                if (x)
                {
                    CreateOrUpdateCornerViewer();
                }
                else
                {
                    CornerViewer = null;
                }
            });

            this.WhenAnyValue(x => x.CornerViewerPercentage)
                .Subscribe(x =>
                {
                    if (CornerViewer != null)
                    {
                        CornerViewer.Percentage = CornerViewerPercentage;
                    }
                });

            ShowExternalViewer = ReactiveCommand.Create(() => Viewer);

            ShowExternalCornerViewer = ReactiveCommand.Create(() =>
            {
                var vm = fitsImageCornerViewerFactory.Create(Viewer);
                vm.Percentage = CornerViewerPercentage;
                return vm;
            });
        }

        private void CreateOrUpdateCornerViewer()
        {
            CornerViewer = fitsImageCornerViewerFactory.Create(Viewer);
            CornerViewer.Percentage = CornerViewerPercentage;
        }

        public void TransferPropertiesFrom(IFitsImageViewerViewModel.IOverlay overlay)
        {
            if (overlay is IAppViewerOverlayViewModel vm)
            {
                CornerViewerPercentage = vm.CornerViewerPercentage;
                IsCornerViewerEnabled = vm.IsCornerViewerEnabled;
            }
        }
    }
}
