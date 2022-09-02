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

using System.Reflection;
using System.Runtime.Loader;

namespace FitsRatingTool.FitsLoader.Native
{
    internal class MappedAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string bootstrapperAssemblyName;
        private readonly string bootstrapperAssemblyPath;
        private readonly Func<string, string?> assemblyMapper;

        internal MappedAssemblyLoadContext(string bootstrapperAssemblyName, string bootstrapperAssemblyPath, Func<string, string?> assemblyMapper)
        {
            this.bootstrapperAssemblyName = bootstrapperAssemblyName;
            this.bootstrapperAssemblyPath = bootstrapperAssemblyPath;
            this.assemblyMapper = assemblyMapper;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == bootstrapperAssemblyName)
            {
                return LoadFromAssemblyPath(bootstrapperAssemblyPath);
            }
            // Fall back to default mechanism
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var mappedName = assemblyMapper.Invoke(unmanagedDllName);
            if (mappedName != null)
            {
                return LoadUnmanagedDllFromPath(mappedName);
            }
            return IntPtr.Zero;
        }
    }
}
