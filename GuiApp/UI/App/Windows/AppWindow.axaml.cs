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

using Avalonia.ReactiveUI;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;
using FitsRatingTool.GuiApp.UI.FitsImage;
using System;
using Avalonia;
using FitsRatingTool.GuiApp.UI.App.ViewModels;
using System.Collections.Generic;
using Avalonia.Controls;
using System.Linq;
using FitsRatingTool.GuiApp.UI.FitsImage.Windows;
using Avalonia.Input;
using System.Reactive.Linq;
using FitsRatingTool.GuiApp.Services;
using FitsRatingTool.GuiApp.UI.JobConfigurator.Windows;
using FitsRatingTool.GuiApp.UI.Evaluation.Windows;
using FitsRatingTool.GuiApp.UI.JobRunner.Windows;
using FitsRatingTool.GuiApp.UI.FileTable.Windows;
using System.Reactive.Disposables;
using FitsRatingTool.GuiApp.UI.Info.Windows;
using Avalonia.Utilities;
using System.IO;
using System.Reactive.Concurrency;
using FitsRatingTool.GuiApp.UI.AppConfig.Windows;
using FitsRatingTool.GuiApp.UI.InstrumentProfile.Windows;
using FitsRatingTool.GuiApp.UI.FileTable;
using FitsRatingTool.GuiApp.UI.Evaluation;
using FitsRatingTool.GuiApp.UI.JobConfigurator;
using FitsRatingTool.GuiApp.UI.JobRunner;
using FitsRatingTool.GuiApp.UI.InstrumentProfile;
using FitsRatingTool.GuiApp.UI.AppConfig;

namespace FitsRatingTool.GuiApp.UI.App.Windows
{
    public partial class AppWindow : ReactiveWindow<AppViewModel>
    {
        private readonly IWindowManager windowManager;

        public AppWindow() : this(null!, null!) { }

        public AppWindow(IWindowManager windowManager, IOpenFileEventManager openFileEventManager)
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.windowManager = windowManager;

            this.WhenActivated(d =>
            {
                d.Add(this.GetObservable(WindowStateProperty).Subscribe(state =>
                {
                    if (state == WindowState.Minimized)
                    {
                        windowManager.MinimizeAll();
                    }
                    else
                    {
                        windowManager.RestoreAll();
                    }
                }));

                if (ViewModel != null)
                {
                    d.Add(ViewModel.Exit.Subscribe(_ => Close()));

                    d.Add(ViewModel.ShowAboutDialog.Subscribe(_ =>
                    {
                        async void showDialog()
                        {
                            await new AboutWindow().ShowDialog(this);
                        }
                        showDialog();
                    }));

                    d.Add(ViewModel.ShowSettingsDialog.Subscribe(factory =>
                    {
                        async void showDialog()
                        {
                            if (windowManager.ShowDialog<AppConfigWindow, IAppConfigViewModel, IAppConfigViewModel.Of>(factory, false, this, out var window, out var task))
                            {
                                await task;
                            }
                        }
                        showDialog();
                    }));

                    d.Add(ViewModel.LoadImagesOpenFileDialog.RegisterHandler(ShowLoadImagesDialogAsync));

                    d.Add(ViewModel.LoadImagesProgressDialog.RegisterHandler(ShowLoadImagesProgressDialogAsync));

                    if (ViewModel.MultiViewer != null)
                    {
                        foreach (var instance in ViewModel.MultiViewer.Instances)
                        {
                            OnViewerLoaded(ViewModel.MultiViewer, new IFitsImageMultiViewerViewModel.ViewerEventArgs(instance.Viewer, false));
                        }

                        ViewModel.MultiViewer.ViewerLoaded += OnViewerLoaded;
                    }

                    d.Add(ViewModel.CalculateAllStatisticsProgressDialog.RegisterHandler(ShowAllStatisticsProgressDialogAsync));

                    d.Add(ViewModel.JobConfiguratorOpenFileDialog.RegisterHandler(ShowJobConfiguratorOpenFileDialogAsync));


                    d.Add(ViewModel.ShowFileTable.Subscribe(factory =>
                    {
                        IDisposable? sub = null;
                        if (windowManager.Show<FileTableWindow, IFileTableViewModel, IFileTableViewModel.Of>(factory.AndThen(vm =>
                        {
                            sub = vm.WhenAnyValue(x => x.SelectedRecord).Subscribe(x =>
                            {
                                if (ViewModel != null && ViewModel.MultiViewer != null && x != null)
                                {
                                    ViewModel.MultiViewer.File = x.File;
                                }
                            });
                        }), false, out var window))
                        {
                            void onClosing(object? sender, EventArgs e)
                            {
                                sub?.Dispose();
                                window.Closing -= onClosing;
                            };
                            window.Closing += onClosing;
                        }
                    }));

                    d.Add(ViewModel.HideFileTable.Subscribe(_ =>
                    {
                        foreach (var w in windowManager.Get<FileTableWindow>()) w.WindowState = WindowState.Minimized;
                        Activate();
                    }));

                    d.Add(ViewModel.ShowEvaluationTable.Subscribe(factory =>
                    {
                        IDisposable? sub = null;
                        if (windowManager.Show<EvaluationTableWindow, IEvaluationTableViewModel, IEvaluationTableViewModel.Of>(factory.AndThen(vm =>
                        {
                            sub = vm.WhenAnyValue(x => x.SelectedRecord).Subscribe(x =>
                            {
                                if (ViewModel != null && ViewModel.MultiViewer != null && x != null)
                                {
                                    ViewModel.MultiViewer.File = x.File;
                                }
                            });
                        }), false, out var window))
                        {
                            void onClosing(object? sender, EventArgs e)
                            {
                                sub?.Dispose();
                                window.Closing -= onClosing;
                            };
                            window.Closing += onClosing;
                        }
                    }));

                    d.Add(ViewModel.ShowEvaluationFormula.Subscribe(factory =>
                    {
                        windowManager.Show<EvaluationFormulaWindow, IEvaluationFormulaViewModel, IEvaluationFormulaViewModel.Of>(factory, false, out var _);
                    }));

                    d.Add(ViewModel.ShowEvaluationTableAndFormula.Subscribe(_ =>
                    {
                        ViewModel.ShowEvaluationTable.Execute().Subscribe();
                        ViewModel.ShowEvaluationFormula.Execute().Subscribe();
                    }));

                    d.Add(ViewModel.HideEvaluationTableAndFormula.Subscribe(_ =>
                    {
                        foreach (var w in windowManager.Get<EvaluationTableWindow>()) w.WindowState = WindowState.Minimized;
                        foreach (var w in windowManager.Get<EvaluationFormulaWindow>()) w.WindowState = WindowState.Minimized;
                        Activate();
                    }));

                    d.Add(ViewModel.ShowEvaluationExporter.Subscribe(factory =>
                    {
                        windowManager.Show<EvaluationExporterWindow, IEvaluationExporterViewModel, IEvaluationExporterViewModel.Of>(factory, false, out var _);
                    }));

                    d.Add(ViewModel.ShowJobConfigurator.Subscribe(factory =>
                    {
                        windowManager.Show<JobConfiguratorWindow, IJobConfiguratorViewModel, IJobConfiguratorViewModel.Of>(factory, false, out var _);
                    }));

                    d.Add(ViewModel.ShowJobConfiguratorWithOpenFileDialog.Subscribe(factory =>
                    {
                        windowManager.Show<JobConfiguratorWindow, IJobConfiguratorViewModel, IJobConfiguratorViewModel.OfConfigFile>(factory, false, out var _);
                    }));

                    d.Add(ViewModel.ShowJobRunner.Subscribe(factory =>
                    {
                        windowManager.Show<JobRunnerWindow, IJobRunnerViewModel, IJobRunnerViewModel.Of>(factory, true, out var _);
                    }));

                    d.Add(ViewModel.ShowInstrumentProfileConfigurator.Subscribe(factory =>
                    {
                        windowManager.Show<InstrumentProfileConfiguratorWindow, IInstrumentProfileConfiguratorViewModel, IInstrumentProfileConfiguratorViewModel.Of>(factory, false, out var _);
                    }));
                }
            });

            AddHandler(DragDrop.DropEvent, DropAsync);
            AddHandler(DragDrop.DragOverEvent, DragOver);

            if (openFileEventManager != null)
            {
                WeakEventHandlerManager.Subscribe<IOpenFileEventManager, IOpenFileEventManager.OpenFileEventArgs, AppWindow>(openFileEventManager, nameof(openFileEventManager.OnOpenFile), OnOpenFile);
            }
        }

        private void OnViewerLoaded(object? sender, IFitsImageMultiViewerViewModel.ViewerEventArgs e)
        {
            var d = new CompositeDisposable();

            var multiViewer = sender as IFitsImageMultiViewerViewModel;
            if (multiViewer == null)
            {
                throw new InvalidOperationException("Sender is not multi viewer VM");
            }

            var viewer = e.Viewer;

            d.Add(viewer.CalculateStatisticsProgressDialog.RegisterHandler(ShowStatisticsProgressDialogAsync));

            void onViewerUnloaded(object? sender, IFitsImageMultiViewerViewModel.ViewerEventArgs e)
            {
                if (e.Viewer == viewer)
                {
                    multiViewer.ViewerUnloaded -= onViewerUnloaded;
                    d.Dispose();
                }
            }
            multiViewer.ViewerUnloaded += onViewerUnloaded;
        }

        private async Task DropAsync(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.FileNames))
            {
                var files = e.Data.GetFileNames();

                if (files != null && DataContext is IAppViewModel vm)
                {
                    await vm.LoadImagesWithProgressDialog.Execute(files);

                    var file = files.FirstOrDefault();
                    if (file != null)
                    {
                        foreach (var item in vm.Items)
                        {
                            if (file.Equals(item.Image.File))
                            {
                                vm.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.FileNames))
            {
                e.DragEffects = DragDropEffects.Move;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        private async Task ShowLoadImagesDialogAsync(InteractionContext<Unit, IEnumerable<string>> ctx)
        {
            OpenFileDialog dialog = new()
            {
                AllowMultiple = true,
                Filters = { new() { Name = "FIT", Extensions = { "fit", "fits", "fts" } } }
            };

            var files = await dialog.ShowAsync(this);

            ctx.SetOutput(files != null ? files.ToList() : new());
        }

        private async Task ShowLoadImagesProgressDialogAsync(InteractionContext<IFitsImageLoadProgressViewModel, Unit> ctx)
        {
            var window = new FitsImageLoadProgressWindow()
            {
                DataContext = ctx.Input
            };

            await window.ShowDialog(this);

            ctx.SetOutput(Unit.Default);
        }

        private async Task ShowStatisticsProgressDialogAsync(InteractionContext<IFitsImageStatisticsProgressViewModel, Unit> ctx)
        {
            var window = new FitsImageStatisticsProgressWindow()
            {
                DataContext = ctx.Input
            };

            await window.ShowDialog(this);

            ctx.SetOutput(Unit.Default);
        }

        private async Task ShowAllStatisticsProgressDialogAsync(InteractionContext<IFitsImageAllStatisticsProgressViewModel, Unit> ctx)
        {
            var window = new FitsImageAllStatisticsProgressWindow()
            {
                DataContext = ctx.Input
            };

            await window.ShowDialog(this);

            ctx.SetOutput(Unit.Default);
        }

        private async Task ShowJobConfiguratorOpenFileDialogAsync(InteractionContext<Unit, string> ctx)
        {
            OpenFileDialog dialog = new()
            {
                Filters = { new() { Name = "Job Config", Extensions = { "json" } } }
            };

            var files = await dialog.ShowAsync(this);

            ctx.SetOutput(files != null && files.Length == 1 ? files[0] : "");
        }

        private void OnOpenFile(object? sender, IOpenFileEventManager.OpenFileEventArgs e)
        {
            if (File.Exists(e.File))
            {
                RxApp.MainThreadScheduler.Schedule(Activate);
            }
        }
    }
}
