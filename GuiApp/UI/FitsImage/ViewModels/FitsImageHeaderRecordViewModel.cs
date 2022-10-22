﻿/*
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

using FitsRatingTool.FitsLoader.Models;
using FitsRatingTool.GuiApp.Services;

namespace FitsRatingTool.GuiApp.UI.FitsImage.ViewModels
{
    public class FitsImageHeaderRecordViewModel : ViewModelBase, IFitsImageHeaderRecordViewModel
    {
        public FitsImageHeaderRecordViewModel(IRegistrar<IFitsImageHeaderRecordViewModel, IFitsImageHeaderRecordViewModel.OfRecord> reg)
        {
            reg.RegisterAndReturn<FitsImageHeaderRecordViewModel>();
        }

        private readonly FitsImageHeaderRecord record;

        // TODO Temp
        public FitsImageHeaderRecordViewModel(IFitsImageHeaderRecordViewModel.OfRecord args)
        {
            this.record = args.Record;
        }

        public string Keyword { get => record.Keyword; }
        public string Value { get => record.Value; }
        public string Comment { get => record.Comment; }
    }
}
