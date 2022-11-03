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
    public interface IFactoryRoot<Instance, Parameter> : IDisposable
        where Instance : class
    {
        /// <summary>
        /// Creates a factory whose instance construction is done by the factory's consumer.
        /// The instances' lifetime is determined entirely by the consumer. The consumer is responsible
        /// for the deconstruction of the instances.
        /// </summary>
        /// <param name="parameterConstructor">Function that returns the parameter to construct the instance from. May return <see langword="null"/> to dispose the factory.</param>
        /// <param name="isSingleUse">Whether the factory should be disposed after the first instantiation.</param>
        /// <returns></returns>
        IParameterizedFactory<Instance, Parameter> Parameterized(Func<Parameter?> parameterConstructor, bool isSingleUse = true);

        /// <summary>
        /// Creates a factory whose instance construction and destruction is delegated to the caller of this method.
        /// The created instances' lifetime is at most this object's lifetime. When this factory root is disposed, all the factories it created are disposed and all the
        /// instances it created are deconstructed. Hence, the caller of this method is responsible for the deconstruction of the created instances by disposing this factory root.
        /// However, the consumer of the factory may and should also trigger the deconstruction of the instances on its own when they're no longer needed.
        /// </summary>
        /// <param name="parameterConstructor">Function that returns the parameter to construct the instance from. May return <see langword="null"/> to dispose the factory.</param>
        /// <param name="instanceConstructor">Function that constructs the instance from a parameter. May return <see langword="null"/> to dispose the factory.</param>
        /// <param name="instanceDestructor">Function that deconstructs an instance created from the factory. Called exactly once for each created instance.</param>
        /// <param name="isSingleUse">Whether the factory should be disposed after the first instantiation.</param>
        /// <returns></returns>
        IDelegatedFactory<Instance, Parameter> Delegated(Func<Parameter?> parameterConstructor, Func<Parameter, Instance?> instanceConstructor, Action<Instance> instanceDestructor, bool isSingleUse = true);
    }
}
