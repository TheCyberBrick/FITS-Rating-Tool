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

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DryIoc;
using System;

namespace FitsRatingTool.GuiApp.UI
{
    public class ViewLocator : IDataTemplate
    {
        private readonly Func<IAppLifecycle?> appLifecycleGetter;

        public ViewLocator(Func<IAppLifecycle?> appLifecycleGetter)
        {
            this.appLifecycleGetter = appLifecycleGetter;
        }

        public IControl Build(object data)
        {
            var appLifecycle = appLifecycleGetter() ?? throw new InvalidOperationException("Cannot locate views until app lifecycle is instantiated");

            var name = data.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            if (type != null)
            {
                if (appLifecycle.Registrator.IsRegistered(type))
                {
                    return (Control)appLifecycle.Resolver.Resolve(type);
                }
                return (Control)Activator.CreateInstance(type)!;
            }
            else
            {
                return new TextBlock { Text = "Not Found: " + name };
            }
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}
