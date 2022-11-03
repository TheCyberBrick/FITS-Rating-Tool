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
using FitsRatingTool.GuiApp.UI.FitsImage;
using FitsRatingTool.IoC;
using ReactiveUI;
using System;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    [Export(typeof(IAppExternalFitsImageViewerViewModel)), TransientReuse]
    public class AppExternalFitsImageViewerViewModel : ViewModelBase, IAppExternalFitsImageViewerViewModel
    {
        public AppExternalFitsImageViewerViewModel(IRegistrar<IAppExternalFitsImageViewerViewModel, IAppExternalFitsImageViewerViewModel.Of> reg)
        {
            reg.RegisterAndReturn<AppExternalFitsImageViewerViewModel>();
        }

        public AppExternalFitsImageViewerViewModel(IRegistrar<IAppExternalFitsImageViewerViewModel, IAppExternalFitsImageViewerViewModel.OfFile> reg)
        {
            reg.RegisterAndReturn<AppExternalFitsImageViewerViewModel>();
        }


        public IAppImageSelectorViewModel Selector { get; private set; } = null!;

        public IFitsImageViewerViewModel Viewer { get; private set; } = null!;


        private AppExternalFitsImageViewerViewModel(
            IAppExternalFitsImageViewerViewModel.Of args,
            IContainer<IAppImageSelectorViewModel, IAppImageSelectorViewModel.Of> appImageSelectorViewModel,
            IContainer<IFitsImageViewerViewModel, IFitsImageViewerViewModel.Of> fitsImageViewerContainer)
            : this((string?)null, appImageSelectorViewModel, fitsImageViewerContainer)
        {
        }

        private AppExternalFitsImageViewerViewModel(
            IAppExternalFitsImageViewerViewModel.OfFile args,
            IContainer<IAppImageSelectorViewModel, IAppImageSelectorViewModel.Of> appImageSelectorViewModel,
            IContainer<IFitsImageViewerViewModel, IFitsImageViewerViewModel.Of> fitsImageViewerContainer)
            : this(args.File, appImageSelectorViewModel, fitsImageViewerContainer)
        {
        }

        private AppExternalFitsImageViewerViewModel(
            string? selectedFile,
            IContainer<IAppImageSelectorViewModel, IAppImageSelectorViewModel.Of> appImageSelectorViewModel,
            IContainer<IFitsImageViewerViewModel, IFitsImageViewerViewModel.Of> fitsImageViewerContainer)
        {
            appImageSelectorViewModel.ToSingleton().Inject(new IAppImageSelectorViewModel.Of(), vm =>
            {
                Selector = vm;
                Selector.SelectedFile = selectedFile;
            });

            fitsImageViewerContainer.ToSingleton().Inject(new IFitsImageViewerViewModel.Of(), vm => Viewer = vm);
        }

        protected override void OnInstantiated()
        {
            this.WhenAnyValue(x => x.Selector.SelectedImage).Subscribe(image => Viewer.FitsImage = image);
        }
    }
}
