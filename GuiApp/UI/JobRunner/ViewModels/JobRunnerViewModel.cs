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

using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;

namespace FitsRatingTool.GuiApp.UI.JobRunner.ViewModels
{
    public class JobRunnerViewModel : ViewModelBase, IJobRunnerViewModel
    {
        public class Factory : IJobRunnerViewModel.IFactory
        {
            private readonly IJobRunnerProgressViewModel.IFactory jobRunnerProgressFactory;

            public Factory(IJobRunnerProgressViewModel.IFactory jobRunnerProgressFactory)
            {
                this.jobRunnerProgressFactory = jobRunnerProgressFactory;
            }

            public IJobRunnerViewModel Create()
            {
                return new JobRunnerViewModel(jobRunnerProgressFactory);
            }
        }



        private string _jobConfigFile = "";
        public string JobConfigFile
        {
            get => _jobConfigFile;
            set => this.RaiseAndSetIfChanged(ref _jobConfigFile, value);
        }

        private string _jobPath = "";
        public string Path
        {
            get => _jobPath;
            set => this.RaiseAndSetIfChanged(ref _jobPath, value);
        }


        public ReactiveCommand<Unit, Unit> SelectJobConfigWithOpenFileDialog { get; }

        public Interaction<Unit, string> SelectJobConfigOpenFileDialog { get; } = new();

        public ReactiveCommand<Unit, Unit> SelectPathWithOpenFolderDialog { get; }

        public Interaction<Unit, string> SelectPathOpenFolderDialog { get; } = new();


        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set => this.RaiseAndSetIfChanged(ref _isRunning, value);
        }

        private IJobRunnerProgressViewModel? _progress;
        public IJobRunnerProgressViewModel? Progress
        {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }

        public ReactiveCommand<Unit, Unit> Run { get; }


        public ReactiveCommand<Unit, IJobRunnerProgressViewModel> RunWithProgress { get; }

        public ReactiveCommand<Unit, Unit> RunWithProgressDialog { get; }

        public Interaction<IJobRunnerProgressViewModel, JobResult> RunProgressDialog { get; } = new();

        public Interaction<JobResult, Unit> RunResultDialog { get; } = new();



        private JobRunnerViewModel(IJobRunnerProgressViewModel.IFactory jobRunnerProgressFactory)
        {
            var canRun = this.WhenAnyValue(x => x.JobConfigFile, x => x.Path, (a, b) => a.Length > 0 && b.Length > 0);

            canRun.Subscribe(x =>
            {
                if (!IsRunning)
                {
                    if (x)
                    {
                        Progress = jobRunnerProgressFactory.Create(JobConfigFile, Path);
                    }
                    else
                    {
                        Progress = null;
                    }
                }
            });

            var canModify = this.WhenAnyValue(x => x.IsRunning, x => !x);

            SelectJobConfigWithOpenFileDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                JobConfigFile = await SelectJobConfigOpenFileDialog.Handle(Unit.Default);
            }, canModify);

            SelectPathWithOpenFolderDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                Path = await SelectPathOpenFolderDialog.Handle(Unit.Default);
            }, canModify);

            Run = ReactiveCommand.CreateFromTask(async () =>
            {
                IsRunning = true;
                try
                {
                    if (Progress != null)
                    {
                        var result = (await Progress.Run.Execute()).Value;

                        if (result != null)
                        {
                            if (result.Error != null)
                            {
                                Debug.WriteLine(result.Error.Message);
                                Debug.WriteLine(result.Error.StackTrace);
                            }

                            try
                            {
                                await RunResultDialog.Handle(result);
                            }
                            catch (UnhandledInteractionException<JobResult, Unit>)
                            {
                                // OK, result dialog is optional
                            }
                        }
                    }

                    Progress = jobRunnerProgressFactory.Create(JobConfigFile, Path);
                }
                finally
                {
                    IsRunning = false;
                }
            }, canRun);

            RunWithProgress = ReactiveCommand.Create(() => jobRunnerProgressFactory.Create(JobConfigFile, Path), canRun);

            RunWithProgressDialog = ReactiveCommand.CreateFromTask(async () =>
            {
                var vm = await RunWithProgress.Execute();
                if (vm != null)
                {
                    var result = await RunProgressDialog.Handle(vm);

                    if (result.Error != null)
                    {
                        Debug.WriteLine(result.Error.Message);
                        Debug.WriteLine(result.Error.StackTrace);
                    }

                    try
                    {
                        await RunResultDialog.Handle(result);
                    }
                    catch (UnhandledInteractionException<JobResult, Unit>)
                    {
                        // OK, result dialog is optional
                    }
                }
            }, canRun);
        }
    }
}
