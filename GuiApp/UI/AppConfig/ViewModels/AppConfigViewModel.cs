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

using DryIocAttributes;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.Evaluation;
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using FitsRatingTool.IoC;
using ReactiveUI;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.AppConfig.ViewModels
{
    [Export(typeof(IAppConfigViewModel)), TransientReuse]
    public class AppConfigViewModel : ViewModelBase, IAppConfigViewModel
    {
        public AppConfigViewModel(IRegistrar<IAppConfigViewModel, IAppConfigViewModel.Of> reg)
        {
            reg.RegisterAndReturn<AppConfigViewModel>();
        }


        public List<IAppConfigCategoryViewModel> Categories { get; } = new();

        public ReactiveCommand<Unit, Unit> Apply { get; }

        public ReactiveCommand<Unit, Unit> SaveAndExit { get; }

        public ReactiveCommand<Unit, Unit> Cancel { get; }


        private readonly IAppConfig appConfig;
        private readonly IContainer<IAppConfigCategoryViewModel, IAppConfigCategoryViewModel.OfName> appConfigCategoryContainer;
        private readonly IContainer<IJobGroupingConfiguratorViewModel, IJobGroupingConfiguratorViewModel.OfConfiguration> jobGroupingConfiguratorContainer;
        private readonly IContainer<IInstrumentProfileSelectorViewModel, IInstrumentProfileSelectorViewModel.Of> instrumentProfileSelectorContainer;

        // Designer only
#pragma warning disable CS8618
        public AppConfigViewModel()
        {
            bool b = false;
            string s = "";

            var category1 = new AppConfigCategoryViewModel(new IAppConfigCategoryViewModel.OfName("Test 1"));
            category1.Settings.Add(new BoolSettingViewModel("Boolean", () => b, v => b = v));
            category1.Settings.Add(new StringSettingViewModel("String", () => s, v => s = v));
            category1.Settings.Add(new StringSettingViewModel("Password", () => s, v => s = v, true));
            category1.Settings.Add(new PathSettingViewModel("File", () => s, v => s = v, PathType.File, new List<string>() { "txt" }));
            category1.Settings.Add(new PathSettingViewModel("Directory", () => s, v => s = v, PathType.Directory));
            Categories.Add(category1);

            Categories.Add(new AppConfigCategoryViewModel(new IAppConfigCategoryViewModel.OfName("Test 2")));
        }
#pragma warning restore CS8618

        private AppConfigViewModel(IAppConfigViewModel.Of args, IAppConfig appConfig, IContainer<IAppConfigCategoryViewModel, IAppConfigCategoryViewModel.OfName> appConfigCategoryContainer,
            IContainer<IJobGroupingConfiguratorViewModel, IJobGroupingConfiguratorViewModel.OfConfiguration> jobGroupingConfiguratorContainer,
            IContainer<IInstrumentProfileSelectorViewModel, IInstrumentProfileSelectorViewModel.Of> instrumentProfileSelectorContainer)
        {
            this.appConfig = appConfig;
            this.appConfigCategoryContainer = appConfigCategoryContainer;
            this.jobGroupingConfiguratorContainer = jobGroupingConfiguratorContainer;
            this.instrumentProfileSelectorContainer = instrumentProfileSelectorContainer;

            appConfigCategoryContainer.BindTo(Categories);

            Apply = ReactiveCommand.Create(CommitAll);
            SaveAndExit = ReactiveCommand.Create(CommitAll);
            Cancel = ReactiveCommand.Create(() => { });
        }

        protected override void OnInstantiated()
        {
            CreateGeneralCategory();
            CreateImagesCategory();
            CreateEvaluationCategory();
            CreateVoyagerCategory();
        }

        private void CommitAll()
        {
            foreach (var category in Categories)
            {
                foreach (var setting in category.Settings)
                {
                    setting.Setting.Commit();
                    setting.Setting.Reset(); // Reset so it is no longer marked as modified
                }
            }
        }

        private IAppConfigCategoryViewModel CreateGeneralCategory()
        {
            var category = appConfigCategoryContainer.Instantiate(new IAppConfigCategoryViewModel.OfName("General"));

            category.Settings.Add(new InstrumentProfileSettingViewModel("Default Profile", () => appConfig.DefaultInstrumentProfileId, v => appConfig.DefaultInstrumentProfileId = v, () => instrumentProfileSelectorContainer.Instantiate(new IInstrumentProfileSelectorViewModel.Of()))
            {
                Description = "Profile that should be used by default."
            });
            category.Settings.Add(new BoolSettingViewModel("Profile Change Confirmation", () => appConfig.InstrumentProfileChangeConfirmation, v => appConfig.InstrumentProfileChangeConfirmation = v)
            {
                Description = "Whether FITS Rating Tool should ask for confirmation before changing profile."
            });
            category.Settings.Add(new BoolSettingViewModel("Open Files In New Window", () => appConfig.OpenFileInNewWindow, v => appConfig.OpenFileInNewWindow = v)
            {
                Description = "Whether files opened through the explorer should be opened in a new window. Changing this setting may require restarting the currently open instance(s) to take effect."
            });


            return category;
        }

        private IAppConfigCategoryViewModel CreateImagesCategory()
        {
            var category = appConfigCategoryContainer.Instantiate(new IAppConfigCategoryViewModel.OfName("Images"));

            category.Settings.Add(new BoolSettingViewModel("Keep Image Data Loaded", () => appConfig.KeepImageDataLoaded, v => appConfig.KeepImageDataLoaded = v)
            {
                Description = "Whether the raw image data should remain loaded in the background. Enabling this option may improve the responsiveness, e.g., when adjusting the image stretch of large images, but increases memory usage."
            });
            category.Settings.Add(SettingSeparatorViewModel.Instance);
            category.Settings.Add(new IntegerSettingViewModel("Max. Auto Load Count", () => appConfig.AutoLoadMaxImageCount, v => appConfig.AutoLoadMaxImageCount = v, 1, 512, 1)
            {
                Description = "Maximum number of automatically loaded images. This includes images opened through the explorer, voyager integration or via program launch argument."
            });
            category.Settings.Add(SettingSeparatorViewModel.Instance);
            category.Settings.Add(new LongSettingViewModel("Max. Image Size (MB)", () => appConfig.MaxImageSize, v => appConfig.MaxImageSize = v, 1, long.MaxValue, 1, 1000000)
            {
                Description = "Maximum image size in megabytes. Images larger than this are not loaded."
            });
            category.Settings.Add(SettingSeparatorViewModel.Instance);
            category.Settings.Add(new IntegerSettingViewModel("Max. Image Width", () => appConfig.MaxImageWidth, v => appConfig.MaxImageWidth = v, 1, int.MaxValue, 128)
            {
                Description = "Images with a width in pixels larger than this number are downscaled when they're loaded."
            });
            category.Settings.Add(new IntegerSettingViewModel("Max. Image Height", () => appConfig.MaxImageHeight, v => appConfig.MaxImageHeight = v, 1, int.MaxValue, 128)
            {
                Description = "Images with a height in pixels larger than this number are downscaled when they're loaded."
            });
            category.Settings.Add(SettingSeparatorViewModel.Instance);
            category.Settings.Add(new IntegerSettingViewModel("Max. Thumbnail Width", () => appConfig.MaxThumbnailWidth, v => appConfig.MaxThumbnailWidth = v, 1, int.MaxValue, 16)
            {
                Description = "Maximum width for image thumbnails."
            });
            category.Settings.Add(new IntegerSettingViewModel("Max. Thumbnail Height", () => appConfig.MaxThumbnailHeight, v => appConfig.MaxThumbnailHeight = v, 1, int.MaxValue, 16)
            {
                Description = "Maximum height for image thumbnails."
            });

            return category;
        }

        private IAppConfigCategoryViewModel CreateEvaluationCategory()
        {
            var category = appConfigCategoryContainer.Instantiate(new IAppConfigCategoryViewModel.OfName("Evaluation"));

            category.Settings.Add(new PathSettingViewModel("Default Evaluation Formula", () => appConfig.DefaultEvaluationFormulaPath, v => appConfig.DefaultEvaluationFormulaPath = v, PathType.File, new List<string>() { "txt" })
            {
                Description = "Path to a text file containing the evaluation formula that should be used by default."
            });
            category.Settings.Add(new BoolSettingViewModel("Automatically Select Group", () => appConfig.AutoSelectGroupKey, v => appConfig.AutoSelectGroupKey = v)
            {
                Description = "Whether the group shown in the evaluation table should be selected automatically based on the currently selected image."
            });
            category.Settings.Add(new BoolSettingViewModel("Enable Dangerous Exporters", () => appConfig.EnableDangerousExporters, v => appConfig.EnableDangerousExporters = v)
            {
                Description = "Whether dangerous exporters (e.g. with non-reversible effects) should be enabled."
            });

            return category;
        }

        private IAppConfigCategoryViewModel CreateVoyagerCategory()
        {
            var category = appConfigCategoryContainer.Instantiate(new IAppConfigCategoryViewModel.OfName("Voyager"));

            category.Settings.Add(new BoolSettingViewModel("Enable Voyager Integration", () => appConfig.VoyagerIntegrationEnabled, v => appConfig.VoyagerIntegrationEnabled = v)
            {
                Description = "Whether the Starkeeper Voyager integration should be enabled. Whenever Voyager saves a new FITS image, it'll also be loaded in FITS Rating Tool."
            });
            category.Settings.Add(new StringSettingViewModel("Address", () => appConfig.VoyagerAddress, v => appConfig.VoyagerAddress = v)
            {
                Description = "Voyager application server address."
            });
            category.Settings.Add(new IntegerSettingViewModel("Port", () => appConfig.VoyagerPort, v => appConfig.VoyagerPort = v, 1, 65535, 0)
            {
                Description = "Voyager application server port."
            });
            category.Settings.Add(new StringSettingViewModel("Username", () => appConfig.VoyagerUsername, v => appConfig.VoyagerUsername = v)
            {
                Description = "Voyager application server username. Leave empty if authentication method 'None' is used."
            });
            category.Settings.Add(new StringSettingViewModel("Password", () => appConfig.VoyagerPassword, v => appConfig.VoyagerPassword = v, true)
            {
                Description = "Voyager application server password. Leave empty if authentication method 'None' is used."
            });
            category.Settings.Add(new StringSettingViewModel("RoboTarget Secret", () => appConfig.RoboTargetSecret, v => appConfig.RoboTargetSecret = v, true)
            {
                Description = "Voyager RoboTarget secret."
            });

            return category;
        }
    }
}
