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
using FitsRatingTool.IoC;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.UI.KeywordPicker.ViewModels
{
    [Export(typeof(IKeywordPickerViewModel)), TransientReuse, AllowDisposableTransient]
    public class KeywordPickerViewModel : ViewModelBase, IKeywordPickerViewModel
    {
        public KeywordPickerViewModel(IRegistrar<IKeywordPickerViewModel, IKeywordPickerViewModel.OfFile> reg)
        {
            reg.RegisterAndReturn<KeywordPickerViewModel>();
        }

        public KeywordPickerViewModel(IRegistrar<IKeywordPickerViewModel, IKeywordPickerViewModel.OfFiles> reg)
        {
            reg.RegisterAndReturn<KeywordPickerViewModel>();
        }

        public KeywordPickerViewModel(IRegistrar<IKeywordPickerViewModel, IKeywordPickerViewModel.OfAllFiles> reg)
        {
            reg.RegisterAndReturn<KeywordPickerViewModel>();
        }

        public KeywordPickerViewModel(IRegistrar<IKeywordPickerViewModel, IKeywordPickerViewModel.OfCurrentlySelectedFile> reg)
        {
            reg.RegisterAndReturn<KeywordPickerViewModel>();
        }


        private enum ResetMode
        {
            None, InitialFiles, AnyFiles, CurrentFile
        }

        private IReadOnlyList<string> _keywords = null!;
        public IReadOnlyList<string> Keywords
        {
            get => _keywords;
            set => this.RaiseAndSetIfChanged(ref _keywords, value);
        }

        private string? _selectedKeyword;
        public string? SelectedKeyword
        {
            get => _selectedKeyword;
            set => this.RaiseAndSetIfChanged(ref _selectedKeyword, value);
        }


        private KeywordPickerViewModel(IKeywordPickerViewModel.OfFile args, IFitsImageManager fitsImageManager, IImageSelectionContext imageSelectionContext)
            : this(fitsImageManager, imageSelectionContext, () => new[] { args.File }, args.AutoReset ? ResetMode.InitialFiles : ResetMode.None)
        {
        }

        private KeywordPickerViewModel(IKeywordPickerViewModel.OfFiles args, IFitsImageManager fitsImageManager, IImageSelectionContext imageSelectionContext)
            : this(fitsImageManager, imageSelectionContext, () => args.Files, args.AutoReset ? ResetMode.InitialFiles : ResetMode.None)
        {
        }

        private KeywordPickerViewModel(IKeywordPickerViewModel.OfAllFiles args, IFitsImageManager fitsImageManager, IImageSelectionContext imageSelectionContext)
            : this(fitsImageManager, imageSelectionContext, () => fitsImageManager.Files, args.AutoReset ? ResetMode.AnyFiles : ResetMode.None)
        {
        }

        private KeywordPickerViewModel(IKeywordPickerViewModel.OfCurrentlySelectedFile args, IFitsImageManager fitsImageManager, IImageSelectionContext imageSelectionContext)
            : this(fitsImageManager, imageSelectionContext, () => new[] { imageSelectionContext.CurrentFile }, args.AutoReset ? ResetMode.CurrentFile : ResetMode.None)
        {
        }


        private readonly IFitsImageManager fitsImageManager;
        private readonly IImageSelectionContext imageSelectionContext;

        private readonly Func<IEnumerable<string?>> filesProvider;
        private readonly ResetMode resetMode;

        private HashSet<string> observedFileSet = new();

        private HashSet<string> keywordSet = new();

        private KeywordPickerViewModel(IFitsImageManager fitsImageManager, IImageSelectionContext imageSelectionContext,
            Func<IEnumerable<string?>> filesProvider, ResetMode resetMode)
        {
            this.fitsImageManager = fitsImageManager;
            this.imageSelectionContext = imageSelectionContext;

            this.filesProvider = filesProvider;
            this.resetMode = resetMode;

            Reset();

            switch (resetMode)
            {
                case ResetMode.CurrentFile:
                    SubscribeToEvent<IImageSelectionContext, IImageSelectionContext.FileChangedEventArgs, KeywordPickerViewModel>(imageSelectionContext, nameof(imageSelectionContext.CurrentFileChanged), OnCurrentFileChanged);
                    goto case ResetMode.InitialFiles;
                case ResetMode.InitialFiles:
                case ResetMode.AnyFiles:
                    SubscribeToEvent<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, KeywordPickerViewModel>(fitsImageManager, nameof(fitsImageManager.RecordChanged), OnRecordChanged);
                    break;
            }
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs e)
        {
            if (e.Type == IFitsImageManager.RecordChangedEventArgs.ChangeType.Metadata &&
                (resetMode == ResetMode.AnyFiles ||
                resetMode == ResetMode.InitialFiles && observedFileSet.Contains(e.File) ||
                resetMode == ResetMode.CurrentFile && e.File == imageSelectionContext.CurrentFile))
            {
                Reset(true);
            }
        }

        private void OnCurrentFileChanged(object? sender, IImageSelectionContext.FileChangedEventArgs e)
        {
            Reset(true);
        }

        public void Reset(bool keepSelection = false)
        {
            var files = filesProvider();

            observedFileSet = new HashSet<string>();
            keywordSet = new HashSet<string>();

            foreach (var file in files)
            {
                if (file != null)
                {
                    if (resetMode == ResetMode.InitialFiles)
                    {
                        observedFileSet.Add(file);
                    }

                    var metadata = fitsImageManager.Get(file)?.Metadata;
                    if (metadata != null)
                    {
                        foreach (var header in metadata.Header)
                        {
                            keywordSet.Add(header.Keyword);
                        }
                    }
                }
            }

            var keywords = new List<string>(keywordSet);

            keywords.Sort();

            using (DelayChangeNotifications())
            {
                Keywords = keywords;
                if (!Select(keepSelection ? SelectedKeyword : null))
                {
                    SelectedKeyword = null;
                }
            }
        }

        public bool Select(string? keyword)
        {
            if (keyword == null)
            {
                SelectedKeyword = null;
                return false;
            }
            else
            {
                if (keywordSet.Contains(keyword))
                {
                    SelectedKeyword = keyword;
                    return true;
                }
            }

            return false;
        }
    }
}
