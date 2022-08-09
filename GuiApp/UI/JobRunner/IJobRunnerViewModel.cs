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
using System.Reactive;

namespace FitsRatingTool.GuiApp.UI.JobRunner
{
    public interface IJobRunnerViewModel
    {
        public interface IFactory
        {
            IJobRunnerViewModel Create();
        }


        string JobConfigFile { get; set; }

        string Path { get; set; }



        ReactiveCommand<Unit, Unit> SelectJobConfigWithOpenFileDialog { get; }

        Interaction<Unit, string> SelectJobConfigOpenFileDialog { get; }

        ReactiveCommand<Unit, Unit> SelectPathWithOpenFolderDialog { get; }

        Interaction<Unit, string> SelectPathOpenFolderDialog { get; }


        bool IsRunning { get; }

        IJobRunnerProgressViewModel? Progress { get; }

        ReactiveCommand<Unit, Unit> Run { get; }


        ReactiveCommand<Unit, IJobRunnerProgressViewModel> RunWithProgress { get; }

        ReactiveCommand<Unit, Unit> RunWithProgressDialog { get; }

        Interaction<IJobRunnerProgressViewModel, JobResult> RunProgressDialog { get; }

        Interaction<JobResult, Unit> RunResultDialog { get; }
    }
}
