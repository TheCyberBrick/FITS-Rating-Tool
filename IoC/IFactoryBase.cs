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

namespace FitsRatingTool.IoC
{
    public interface IFactoryBase<out Instance>
    {
        bool IsSingleUse { get; }

        Type InstanceType { get; }

        Type ParameterType { get; }

        /// <summary>
        /// Returns a child factory that invokes the specified <paramref name="action"/>
        /// upon instantiation.
        /// </summary>
        /// <param name="action">Action to invoke upon instantiation.</param>
        /// <returns></returns>
        IFactoryBase<Instance> AndThen(Action<Instance> action);
    }
}
