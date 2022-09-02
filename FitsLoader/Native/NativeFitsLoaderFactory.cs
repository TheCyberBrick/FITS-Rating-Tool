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

namespace FitsRatingTool.FitsLoader.Native
{
    public class NativeFitsLoaderFactory
    {
        private const string NativeFitsLoaderBootstrapperAssembly = "NativeFitsLoaderBootstrapper";
        private const string NativeFitsLoaderBootstrapperType = "NativeFitsLoaderBootstrapper";

        private static readonly string AssemblyPath = Path.GetDirectoryName(typeof(NativeFitsLoaderFactory).Assembly.Location) ?? "";

        private static readonly MappedAssemblyLoadContext alc = new(
                NativeFitsLoaderBootstrapperAssembly,
                Path.Combine(AssemblyPath, NativeFitsLoaderBootstrapperAssembly + ".dll"),
                MapAssembly);

        public static INativeFitsLoader Create()
        {
            var assembly = alc.LoadFromAssemblyName(new AssemblyName(NativeFitsLoaderBootstrapperAssembly));
            var bootstrapperType = assembly.GetType(NativeFitsLoaderBootstrapperType);
            if (bootstrapperType == null)
            {
                throw new InvalidOperationException("Could not find bootstrapper type '" + NativeFitsLoaderBootstrapperType + "'");
            }
            var itf = Activator.CreateInstance(bootstrapperType) as INativeFitsLoader;
            if (itf == null)
            {
                throw new InvalidOperationException("Bootstrapper does not implement '" + nameof(INativeFitsLoader) + "'");
            }
            return itf;
        }

        private static string? MapAssembly(string assemblyName)
        {
            if (assemblyName == "NativeFitsLoader")
            {
                assemblyName = Environment.Is64BitProcess ? "NativeFitsLoader.dll" : "NativeFitsLoader32.dll";
                return Path.Combine(AssemblyPath, assemblyName);
            }
            return null;
        }
    }
}
