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
using static FitsRatingTool.GuiApp.UI.FitsImage.IFitsImageViewerViewModel;

namespace FitsRatingTool.GuiApp.UI.App
{
    public class AppViewerOverlayFactory : IOverlayFactory
    {
        private readonly IContainer<IAppViewerOverlayViewModel, IAppViewerOverlayViewModel.OfViewer> container;

        public AppViewerOverlayFactory(IContainer<IAppViewerOverlayViewModel, IAppViewerOverlayViewModel.OfViewer> container)
        {
            this.container = container;
        }

        public IOverlay Create(IFitsImageViewerViewModel viewer)
        {
            return container.Instantiate(new IAppViewerOverlayViewModel.OfViewer(viewer));
        }

        public void Destroy(IOverlay overlay)
        {
            if (overlay is IAppViewerOverlayViewModel vm)
            {
                container.Destroy(vm);
            }
        }
    }
}
