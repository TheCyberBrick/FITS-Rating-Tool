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
using System.Diagnostics;
using FitsRatingTool.GuiApp.UI.Evaluation.Windows;
using FitsRatingTool.GuiApp.UI.JobRunner.Windows;
using FitsRatingTool.GuiApp.UI.MessageBox.Windows;
using FitsRatingTool.GuiApp.UI.MessageBox.ViewModels;
using FitsRatingTool.GuiApp.UI.FileTable.Windows;
using System.Reactive.Disposables;
using FitsRatingTool.GuiApp.UI.Info.Windows;
using Avalonia.Utilities;
using System.IO;
using System.Reactive.Concurrency;
using FitsRatingTool.GuiApp.UI.AppConfig.Windows;
using FitsRatingTool.GuiApp.UI.InstrumentProfile.Windows;

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

                    d.Add(ViewModel.ShowSettingsDialog.Subscribe(vm =>
                    {
                        async void showDialog()
                        {
                            await new AppConfigWindow()
                            {
                                DataContext = vm
                            }.ShowDialog(this);
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


                    d.Add(ViewModel.ShowFileTable.Subscribe(vm =>
                    {
                        windowManager.Show(() =>
                        {
                            var window = new FileTableWindow()
                            {
                                DataContext = vm
                            };

                            var sub = vm.WhenAnyValue(x => x.SelectedRecord).Subscribe(x =>
                            {
                                if (ViewModel != null && ViewModel.MultiViewer != null && x != null)
                                {
                                    ViewModel.MultiViewer.File = x.File;
                                }
                            });

                            void onClosing(object? sender, EventArgs e)
                            {
                                sub.Dispose();
                                window.Closing -= onClosing;
                            };
                            window.Closing += onClosing;

                            return window;
                        }, false);
                    }));

                    d.Add(ViewModel.HideFileTable.Subscribe(_ =>
                    {
                        foreach (var w in windowManager.Get<FileTableWindow>()) w.WindowState = WindowState.Minimized;
                        Activate();
                    }));

                    d.Add(ViewModel.ShowEvaluationTable.Subscribe(vm =>
                    {
                        windowManager.Show(() =>
                        {
                            var window = new EvaluationTableWindow()
                            {
                                DataContext = vm
                            };

                            var sub = vm.WhenAnyValue(x => x.SelectedRecord).Subscribe(x =>
                            {
                                if (ViewModel != null && ViewModel.MultiViewer != null && x != null)
                                {
                                    ViewModel.MultiViewer.File = x.File;
                                }
                            });

                            void onClosing(object? sender, EventArgs e)
                            {
                                sub.Dispose();
                                window.Closing -= onClosing;
                            };
                            window.Closing += onClosing;

                            return window;
                        }, false);
                    }));

                    d.Add(ViewModel.ShowEvaluationFormula.Subscribe(vm =>
                    {
                        windowManager.Show(() => new EvaluationFormulaWindow()
                        {
                            DataContext = vm
                        }, false);
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

                    d.Add(ViewModel.ShowEvaluationExporter.Subscribe(vm =>
                    {
                        windowManager.Show(() => new EvaluationExporterWindow()
                        {
                            DataContext = vm
                        }, false);
                    }));

                    d.Add(ViewModel.ShowJobConfigurator.Subscribe(vm =>
                    {
                        windowManager.Show(() => new JobConfiguratorWindow()
                        {
                            DataContext = vm
                        }, false);
                    }));

                    d.Add(ViewModel.ShowJobConfiguratorWithOpenFileDialog.Subscribe(result =>
                    {
                        if (result.JobConfigurator != null)
                        {
                            var window = windowManager.Get<JobConfiguratorWindow>().FirstOrDefault();
                            if (window != null)
                            {
                                window.Close();
                            }

                            windowManager.Show(() => new JobConfiguratorWindow()
                            {
                                DataContext = result.JobConfigurator
                            }, false);
                        }
                        else
                        {
                            Debug.WriteLine(result.Error);

                            MessageBoxWindow.Show(this, MessageBoxStyle.Ok, "Import Failed", result.Error != null ? result.Error.Message : "Unknown error", null, MessageBoxIcon.Error);
                        }
                    }));

                    d.Add(ViewModel.ShowJobRunner.Subscribe(vm =>
                    {
                        windowManager.Show(() => new JobRunnerWindow()
                        {
                            DataContext = vm
                        }, true);
                    }));

                    d.Add(ViewModel.ShowInstrumentProfileConfigurator.Subscribe(vm =>
                    {
                        windowManager.Show(() => new InstrumentProfileConfiguratorWindow()
                        {
                            DataContext = vm
                        }, false);
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
