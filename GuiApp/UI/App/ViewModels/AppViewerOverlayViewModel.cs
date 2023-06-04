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

using DryIocAttributes;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.FitsImage;
using FitsRatingTool.GuiApp.UI.ImageAnalysis;
using FitsRatingTool.IoC;
using ReactiveUI;
using System;
using System.ComponentModel.Composition;
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    [Export(typeof(IAppViewerOverlayViewModel)), TransientReuse]
    public class AppViewerOverlayViewModel : ViewModelBase, IAppViewerOverlayViewModel
    {
        public AppViewerOverlayViewModel(IRegistrar<IAppViewerOverlayViewModel, IAppViewerOverlayViewModel.Of> reg)
        {
            reg.RegisterAndReturn<AppViewerOverlayViewModel>();
        }


        private IFitsImageViewerViewModel? _viewer;
        public IFitsImageViewerViewModel? Viewer
        {
            get => _viewer;
            set => this.RaiseAndSetIfChanged(ref _viewer, value);
        }


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

        public ReactiveCommand<Unit, IParameterizedFactory<IAppExternalFitsImageViewerViewModel, IAppExternalFitsImageViewerViewModel.OfFile>> ShowExternalViewer { get; }

        public ReactiveCommand<Unit, IParameterizedFactory<IFitsImageCornerViewerViewModel, IFitsImageCornerViewerViewModel.OfViewer>> ShowExternalCornerViewer { get; }

        public ReactiveCommand<Unit, IParameterizedFactory<IImageAnalysisViewModel, IImageAnalysisViewModel.OfFile>> ShowExternalImageAnalysis { get; }


        private readonly ISingletonContainer<IFitsImageCornerViewerViewModel, IFitsImageCornerViewerViewModel.OfViewer> fitsImageCornerViewerContainer;

        private AppViewerOverlayViewModel(IAppViewerOverlayViewModel.Of args,
            IFitsImageManager fitsImageManager,
            IContainer<IFitsImageCornerViewerViewModel, IFitsImageCornerViewerViewModel.OfViewer> fitsImageCornerViewerContainer,
            IFactoryRoot<IAppExternalFitsImageViewerViewModel, IAppExternalFitsImageViewerViewModel.OfFile> fitsImageViewerFactory,
            IFactoryRoot<IFitsImageCornerViewerViewModel, IFitsImageCornerViewerViewModel.OfViewer> fitsImageCornerViewerFactory,
            IFactoryRoot<IImageAnalysisViewModel, IImageAnalysisViewModel.OfFile> imageAnalysisFactory)
        {
            this.fitsImageCornerViewerContainer = fitsImageCornerViewerContainer.Singleton();
            this.fitsImageCornerViewerContainer.Subscribe(vm => CornerViewer = vm);

            this.WhenAnyValue(x => x.Viewer!.File).Subscribe(x =>
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

            this.WhenAnyValue(x => x.Viewer).Subscribe(_ => UpdateCornerViewer());
            this.WhenAnyValue(x => x.IsCornerViewerEnabled).Subscribe(_ => UpdateCornerViewer());

            this.WhenAnyValue(x => x.CornerViewerPercentage)
                .Subscribe(x =>
                {
                    if (CornerViewer != null)
                    {
                        CornerViewer.Percentage = CornerViewerPercentage;
                    }
                });

            ShowExternalViewer = ReactiveCommand.Create(() => fitsImageViewerFactory.Parameterized(new IAppExternalFitsImageViewerViewModel.OfFile(Viewer?.File)));

            // TODO Temp
            // Need to reimplement these properlywith image selector VM
            // vvv

            // TODO Temp
            // See above
            ShowExternalCornerViewer = ReactiveCommand.Create(() => fitsImageCornerViewerFactory.Parameterized(new IFitsImageCornerViewerViewModel.OfViewer(Viewer)).AndThen(vm =>
            {
                vm.Percentage = CornerViewerPercentage;
            }));

            // TODO Temp
            // See above
            ShowExternalImageAnalysis = ReactiveCommand.Create(() => imageAnalysisFactory.Parameterized(new IImageAnalysisViewModel.OfFile(Viewer.File!)), this.WhenAnyValue(x => x.Viewer.File, (string? x) => x != null));
        }

        private void UpdateCornerViewer()
        {
            if (IsCornerViewerEnabled && Viewer != null)
            {
                var cornerViewer = fitsImageCornerViewerContainer.Instantiate(new IFitsImageCornerViewerViewModel.OfViewer(Viewer));
                cornerViewer.Percentage = CornerViewerPercentage;
            }
            else
            {
                fitsImageCornerViewerContainer.Destroy();
            }
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
