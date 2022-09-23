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

using FitsRatingTool.GuiApp.UI.FitsImage;
using ReactiveUI;
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.App.ViewModels
{
    public class AppImageItemViewModel : ViewModelBase, IAppImageItemViewModel
    {
        public class Factory : IAppImageItemViewModel.IFactory
        {
            public IAppImageItemViewModel Create(long id, IFitsImageViewModel image)
            {
                return new AppImageItemViewModel(id, image);
            }
        }

        public long Id { get; }

        public long IdPlusOne => Id + 1;

        private float _scale = 1.0f;
        public float Scale
        {
            get => _scale;
            set => this.RaiseAndSetIfChanged(ref _scale, value);
        }

        public IFitsImageViewModel Image { get; }

        public ReactiveCommand<Unit, Unit> Remove { get; }


        public AppImageItemViewModel(long id, IFitsImageViewModel image)
        {
            Id = id;
            Image = image;
            Remove = ReactiveCommand.Create(() => { });
        }
    }
}
