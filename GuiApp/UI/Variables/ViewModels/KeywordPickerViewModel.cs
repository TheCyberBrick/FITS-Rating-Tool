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
using FitsRatingTool.GuiApp.UI.App;
using FitsRatingTool.IoC;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FitsRatingTool.GuiApp.UI.Variables.ViewModels
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


        private KeywordPickerViewModel(IKeywordPickerViewModel.OfFile args, IFitsImageManager fitsImageManager)
            : this(fitsImageManager, () => new[] { args.File }, args.AutoReset, false)
        {
        }

        private KeywordPickerViewModel(IKeywordPickerViewModel.OfFiles args, IFitsImageManager fitsImageManager)
            : this(fitsImageManager, () => args.Files, args.AutoReset, false)
        {
        }

        private KeywordPickerViewModel(IKeywordPickerViewModel.OfAllFiles args, IFitsImageManager fitsImageManager)
            : this(fitsImageManager, () => fitsImageManager.Files, args.AutoReset, true)
        {
        }

        private KeywordPickerViewModel(IKeywordPickerViewModel.OfCurrentlySelectedFile args, IFitsImageManager fitsImageManager)
            : this(fitsImageManager, () => new[] { fitsImageManager.CurrentFile }, args.AutoReset, false)
        {
        }


        private readonly IFitsImageManager fitsImageManager;
        private readonly Func<IEnumerable<string?>> filesProvider;
        private readonly bool autoReset;


        private readonly bool observeAllFiles;
        private HashSet<string> observedFileSet = new();

        private HashSet<string> keywordSet = new();

        private KeywordPickerViewModel(IFitsImageManager fitsImageManager, Func<IEnumerable<string?>> filesProvider, bool autoReset, bool alwaysReset)
        {
            this.fitsImageManager = fitsImageManager;
            this.filesProvider = filesProvider;
            this.autoReset = autoReset;
            this.observeAllFiles = alwaysReset;

            Reset();

            if (autoReset)
            {
                SubscribeToEvent<IFitsImageManager, IFitsImageManager.RecordChangedEventArgs, KeywordPickerViewModel>(fitsImageManager, nameof(fitsImageManager.RecordChanged), OnRecordChanged);
            }
        }

        private void OnRecordChanged(object? sender, IFitsImageManager.RecordChangedEventArgs e)
        {
            if (e.Type == IFitsImageManager.RecordChangedEventArgs.DataType.Metadata && (observeAllFiles || observedFileSet.Contains(e.File)))
            {
                Reset(true);
            }
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
                    if (autoReset && !observeAllFiles)
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
                Select(keepSelection ? SelectedKeyword : null);
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
