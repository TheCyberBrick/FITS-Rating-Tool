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
using DryIocAttributes;
using System;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    [Export(typeof(IImageSelectionContext)), CurrentScopeReuse(AppScopes.Context.ImageSelection)]
    public class ImageSelectionContext : IImageSelectionContext
    {
        private readonly IFitsImageManager fitsImageManager;

        public ImageSelectionContext(IFitsImageManager fitsImageManager)
        {
            this.fitsImageManager = fitsImageManager;

            WeakEventHandlerManager.Subscribe<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, ImageSelectionContext>(fitsImageManager, nameof(fitsImageManager.RecordChanged), OnRecordChanged);
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs args)
        {
            var currentFile = CurrentFile;
            var currentRecord = CurrentRecord;

            if (currentFile != null && currentRecord != null && args.File == currentFile)
            {
                if (args.AddedOrUpdated && args.Type != IFitsImageManager.RecordChangedEventArgs.ChangeType.File)
                {
                    // If the data in the record has changed we also notify with
                    // the appropriate change type
                    _currentRecordChanged?.Invoke(this, new IImageSelectionContext.RecordChangedEventArgs(currentRecord, currentRecord, args.Type));
                }
                else if (args.Removed)
                {
                    // Reset current selection if the current record/file is no
                    // longer available
                    CurrentRecord = null;
                    CurrentFile = null;
                }
            }
        }

        public void LoadFromOther(IImageSelectionContext ctx)
        {
            CurrentRecord = ctx.CurrentRecord;
        }


        private string? _currentFile;
        public string? CurrentFile
        {
            get => _currentFile;
            set
            {
                if (_currentFile != value)
                {
                    IFitsImageManager.IRecord? record = null;
                    if (value != null)
                    {
                        record = fitsImageManager.Get(value);
                        if (record == null)
                        {
                            throw new InvalidOperationException("Unknown file '" + value + "'");
                        }
                    }

                    var old = _currentFile;
                    _currentFile = value;

                    CurrentRecord = record;

                    _currentFileChanged?.Invoke(this, new IImageSelectionContext.FileChangedEventArgs(old, value));
                }
            }
        }

        public string? CurrentFileName => CurrentRecord?.FileName;

        private IFitsImageManager.IRecord? _currentRecord;
        public IFitsImageManager.IRecord? CurrentRecord
        {
            get => _currentRecord;
            set
            {
                if (_currentRecord != value)
                {
                    if (value != null)
                    {
                        var record = fitsImageManager.Get(value.File);
                        if (record != value)
                        {
                            throw new InvalidOperationException("Unkown record for file '" + value.File + "'");
                        }
                    }

                    var old = _currentRecord;
                    _currentRecord = value;

                    CurrentFile = value?.File;

                    _currentRecordChanged?.Invoke(this, new IImageSelectionContext.RecordChangedEventArgs(old, value, IFitsImageManager.RecordChangedEventArgs.ChangeType.File));
                }
            }
        }


        private EventHandler<IImageSelectionContext.FileChangedEventArgs>? _currentFileChanged;
        public event EventHandler<IImageSelectionContext.FileChangedEventArgs> CurrentFileChanged
        {
            add => _currentFileChanged += value;
            remove => _currentFileChanged -= value;
        }

        private EventHandler<IImageSelectionContext.RecordChangedEventArgs>? _currentRecordChanged;
        public event EventHandler<IImageSelectionContext.RecordChangedEventArgs> CurrentRecordChanged
        {
            add => _currentRecordChanged += value;
            remove => _currentRecordChanged -= value;
        }
    }
}
