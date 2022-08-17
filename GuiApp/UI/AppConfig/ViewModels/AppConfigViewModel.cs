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
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.AppConfig.ViewModels
{
    public class AppConfigViewModel : IAppConfigViewModel
    {
        public class Factory : IAppConfigViewModel.IFactory
        {
            private readonly IAppConfig appConfig;
            private readonly IAppConfigCategoryViewModel.IFactory appConfigCategoryFactory;

            public Factory(IAppConfig appConfig, IAppConfigCategoryViewModel.IFactory appConfigCategoryFactory)
            {
                this.appConfig = appConfig;
                this.appConfigCategoryFactory = appConfigCategoryFactory;
            }

            public IAppConfigViewModel Create()
            {
                return new AppConfigViewModel(appConfig, appConfigCategoryFactory);
            }
        }


        public List<IAppConfigCategoryViewModel> Categories { get; } = new();

        public ReactiveCommand<Unit, Unit> Apply { get; }

        public ReactiveCommand<Unit, Unit> SaveAndExit { get; }

        public ReactiveCommand<Unit, Unit> Cancel { get; }


        private readonly IAppConfig appConfig;
        private readonly IAppConfigCategoryViewModel.IFactory appConfigCategoryFactory;

        // Designer only
#pragma warning disable CS8618
        public AppConfigViewModel()
        {
            bool b = false;
            string s = "";

            var category1 = new AppConfigCategoryViewModel("Test 1");
            category1.Settings.Add(new BoolSettingViewModel("Boolean", () => b, v => b = v));
            category1.Settings.Add(new StringSettingViewModel("String", () => s, v => s = v));
            category1.Settings.Add(new StringSettingViewModel("Password", () => s, v => s = v, true));
            Categories.Add(category1);

            Categories.Add(new AppConfigCategoryViewModel("Test 2"));
        }
#pragma warning restore CS8618

        public AppConfigViewModel(IAppConfig appConfig, IAppConfigCategoryViewModel.IFactory appConfigCategoryFactory)
        {
            this.appConfig = appConfig;
            this.appConfigCategoryFactory = appConfigCategoryFactory;

            Categories.Add(CreateGeneralCategory());
            Categories.Add(CreateVoyagerCategory());

            Apply = ReactiveCommand.Create(CommitAll);
            SaveAndExit = ReactiveCommand.Create(CommitAll);
            Cancel = ReactiveCommand.Create(() => { });
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
            var category = appConfigCategoryFactory.Create("General");

            category.Settings.Add(new IntegerSettingViewModel("Max. Auto Load Count", () => appConfig.AutoLoadMaxImageCount, v => appConfig.AutoLoadMaxImageCount = v, 1, 512, 1)
            {
                Description = "Maximum number of automatically loaded images. This includes images opened via program launch argument."
            });

            return category;
        }

        private IAppConfigCategoryViewModel CreateVoyagerCategory()
        {
            var category = appConfigCategoryFactory.Create("Voyager");

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
