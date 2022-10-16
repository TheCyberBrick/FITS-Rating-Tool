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

using System;
using System.Collections.Generic;

namespace FitsRatingTool.GuiApp.Services
{
    public static class IContainerExtensions
    {
        public static T Inject<T, Template>(this IContainer<T, Template> container, Template template)
            where T : class
        {
            return container.Instantiate(template).Instance;
        }

        public static T? Inject<T, Template>(this IContainer<T, Template> container, Template template, bool allowNull = true)
            where T : class
        {
            return container.Instantiate(template).InstanceOrNull;
        }

        public static void Inject<T, Template>(this IContainer<T, Template> container, Template template, Action<T> consumer)
            where T : class
        {
            Inject(container, template, (Action<T?>)consumer, false);
        }

        public static void Inject<T, Template>(this IContainer<T, Template> container, Template template, Action<T?> consumer, bool allowNull = true)
            where T : class
        {
            container.Instantiate(template);

            if (!container.IsInitialized)
            {
                void onInitialized(Template? initTemplate)
                {
                    if (EqualityComparer<Template>.Default.Equals(template, initTemplate))
                    {
                        consumer.Invoke(allowNull ? container.InstanceOrNull : container.Instance);
                    }
                    container.OnInitialized -= onInitialized;
                }
                container.OnInitialized += onInitialized;
            }
            else
            {
                consumer.Invoke(allowNull ? container.InstanceOrNull : container.Instance);
            }
        }
    }
}
