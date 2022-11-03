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

using System.Collections.Specialized;
using System.ComponentModel;

namespace FitsRatingTool.IoC
{
    public interface IReadOnlyContainer<T> : IReadOnlyCollection<T>, INotifyPropertyChanged, INotifyPropertyChanging, INotifyCollectionChanged
        where T : class
    {
        bool IsInitialized { get; }

        event Action OnInitialized;

        event Action<T> OnInstantiated;

        event Action<T> OnDestroyed;

        bool IsSingleton { get; }
    }
}
