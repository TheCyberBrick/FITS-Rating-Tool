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

using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.FitsImage;
using FitsRatingTool.GuiApp.UI.ImageAnalysis;
using ReactiveUI;
using System;
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    public class AppViewerOverlayViewModel : ViewModelBase, IAppViewerOverlayViewModel
    {
        public AppViewerOverlayViewModel(IRegistrar<IAppViewerOverlayViewModel, IAppViewerOverlayViewModel.OfViewer> reg)
        {
            reg.RegisterAndReturn<AppViewerOverlayViewModel>();
        }


        public IFitsImageViewerViewModel Viewer { get; }


        private long _fileId;
        public long FileId
        {
            get => _fileId;
            private set
            {
                if (_fileId != value)
                {
                    this.RaiseAndSetIfChanged(ref _fileId, value);
                    this.RaiseAndSetIfChanged(ref _fileIdPlusOne, value + 1, nameof(FileIdPlusOne));
                }
            }
        }

        private long _fileIdPlusOne;
        public long FileIdPlusOne => _fileIdPlusOne;

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

        public ReactiveCommand<Unit, IImageAnalysisViewModel> ShowExternalImageAnalysis { get; }


        private AppViewerOverlayViewModel(IAppViewerOverlayViewModel.OfViewer args, IContainer<IFitsImageCornerViewerViewModel, IFitsImageCornerViewerViewModel.OfViewer> fitsImageCornerViewerContainer,
            IContainer<IImageAnalysisViewModel, IImageAnalysisViewModel.OfFile> imageAnalysisContainer, IFitsImageManager fitsImageManager)
        {
            Viewer = args.Viewer;

            fitsImageCornerViewerContainer.ToSingletonWithObservable().Subscribe(vm => CornerViewer = vm);

            this.WhenAnyValue(x => x.Viewer.File).Subscribe(x =>
            {
                if (x != null)
                {
                    FileId = fitsImageManager.Get(x)?.Id ?? -1;
                }
                else
                {
                    FileId = -1;
                }
            });

            this.WhenAnyValue(x => x.IsCornerViewerEnabled).Subscribe(x =>
            {
                if (x)
                {
                    var cornerViewer = fitsImageCornerViewerContainer.Instantiate(new IFitsImageCornerViewerViewModel.OfViewer(Viewer));
                    cornerViewer.Percentage = CornerViewerPercentage;
                }
                else
                {
                    fitsImageCornerViewerContainer.Destroy();
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

            // TODO Temp
            ShowExternalCornerViewer = ReactiveCommand.Create(() =>
            {
                var vm = fitsImageCornerViewerContainer.Instantiate(new IFitsImageCornerViewerViewModel.OfViewer(Viewer));
                vm.Percentage = CornerViewerPercentage;
                return vm;
            });

            // TODO Temp
            ShowExternalImageAnalysis = ReactiveCommand.Create(() => imageAnalysisContainer.Instantiate(new IImageAnalysisViewModel.OfFile(Viewer.File!)), this.WhenAnyValue(x => x.Viewer.File, (string? x) => x != null));
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
