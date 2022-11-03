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
    /// <summary>
    /// Use this interface to gain access to all registered <seealso cref="IComponentRegistration{Base}"/>'s with the same <typeparamref name="Base"/> type.
    /// This registry must be instantiated through an <see cref="IContainer{Instance, Parameter}"/>.
    /// </summary>
    /// <typeparam name="Base"></typeparam>
    public interface IComponentRegistry<Base>
        where Base : class
    {
        /// <summary>
        /// Thrown when a registration failed.
        /// </summary>
        public class RegistrationFailedException : Exception
        {
            public string Id { get; }

            public RegistrationFailedException(string id, string message) : base(message)
            {
                Id = id;
            }
        }

        /// <summary>
        /// Parameter for the instantiation of a registry without any additional registrations.
        /// </summary>
        public record Of();

        /// <summary>
        /// Parameter for the instantiation of a registry with additional registrations.
        /// </summary>
        /// <param name="Registrations"></param>
        /// <exception cref="RegistrationFailedException">When the registration of one of the additional registrations failed. See <seealso cref="Register(IComponentRegistration{Base})"/>.</exception>
        public record OfRegistrations(params IComponentRegistration<Base>[] Registrations);

        /// <summary>
        /// Base type of this registry.
        /// </summary>
        Type BaseType => typeof(Base);

        /// <summary>
        /// IDs of all found <see cref="IComponentRegistration{Base}"/>'s with the same <typeparamref name="Base"/> type.
        /// </summary>
        IEnumerable<string> Ids { get; }

        /// <summary>
        /// Returns the registration with the given ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IComponentRegistration<Base>? GetRegistration(string id);

        /// <summary>
        /// Returns the factory of the registration with the given ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IDelegatedFactory<Base>? GetFactory(string id);

        /// <summary>
        /// Registers an additional <see cref="IComponentRegistration{Base}"/>.
        /// </summary>
        /// <param name="registration"></param>
        /// <returns>True if the registration was successful. False otherwise, e.g., if there already is a registration with the same ID.</returns>
        bool Register(IComponentRegistration<Base> registration);
    }
}
