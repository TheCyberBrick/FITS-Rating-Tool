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

using System;

namespace FitsRatingTool.GuiApp.Services
{
    public interface IContainerRoot<T, Template>
        where T : class
    {
        IDisposable Initialize(out IContainer<T, Template> container);

        IDisposable Instantiate(Template template, out IContainer<T, Template> container);
    }
}
