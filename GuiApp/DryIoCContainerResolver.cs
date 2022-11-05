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

using DryIoc;
using System;
using System.Reflection;

namespace FitsRatingTool.IoC.Impl
{
    public class DryIoCContainerResolver : IContainerResolver
    {
        private class Scope : IContainerResolver.IScope
        {
            public IContainerResolver Resolver => ioc;

            public object Key => ctx;

            private readonly IContainerResolver ioc;
            private readonly IResolverContext ctx;

            public Scope(IContainerResolver ioc, IResolverContext ctx)
            {
                this.ioc = ioc;
                this.ctx = ctx;
            }

            public Service Resolve<Service, Parameter>(Parameter parameter)
            {
                return ctx.Resolve<Func<Parameter, IContainerResolver, IContainerResolver.IScope, Service>>().Invoke(parameter, ioc, this);
            }

            public void Dispose()
            {
                ctx.Dispose();
            }
        }

        private readonly IContainer container;

        public DryIoCContainerResolver(IContainer container)
        {
            this.container = container;
        }

        public object GetClassScopeName<T>()
        {
            return ResolutionScopeName.Of<T>();
        }

        public IContainerResolver.IScope OpenScope(object? scopeName)
        {
            return new Scope(this, container.OpenScope(scopeName));
        }

        public IContainerResolver Fork<Service, Implementation>(ConstructorInfo? ctor, Action<IContainerLifecycle, IContainerResolver.IScope> initializer, Predicate<object> scopeKeyPredicate) where Implementation : Service
        {
            // Create child container
            var childContainer = container.With(
                container,
                container.Rules.WithDefaultIfAlreadyRegistered(IfAlreadyRegistered.Replace),
                container.ScopeContext,
                RegistrySharing.CloneButKeepCache,
                container.SingletonScope,
                container.CurrentScope,
                IsRegistryChangePermitted.Permitted);

            // Register initializer
            childContainer.Register<object>(
                made: Made.Of(
                    req => typeof(Initializer)
                        .SingleMethod(nameof(Initializer.CreateAndInitialize))
                        .MakeGenericMethod(req.ServiceType.GetGenericArguments()),
                    parameters: Parameters.Of.Type(req => initializer)),
                setup: Setup.DecoratorWith(
                    r => scopeKeyPredicate.Invoke(r.Container)
                        && r.FactoryType != FactoryType.Wrapper
                        && r.ServiceType.IsGenericType
                        && typeof(IContainer<,>).IsAssignableFrom(r.ServiceType.GetGenericTypeDefinition()),
                    useDecorateeReuse: true,
                    preventDisposal: true)
                );

            // Register service
            if (ctor != null)
            {
                childContainer.Register<Service, Implementation>(reuse: Reuse.Scoped, made: Made.Of(ctor));
            }
            else
            {
                childContainer.Register<Service, Implementation>(reuse: Reuse.Scoped, made: Made.Of(FactoryMethod.Constructor(mostResolvable: true, includeNonPublic: true)));
            }

            return new DryIoCContainerResolver(childContainer);
        }

        public void DestroyFork()
        {
            // Child container is not disposed because
            // disposal of services is already handled
            // by the scope, and disposing the child
            // container would cause unintentional
            // disposal of other services (such as
            // singletons) due to the container's
            // scopes being shared instead of cloned
        }

        private static class Initializer
        {
            public static IContainer<DInstance, DParameter> CreateAndInitialize<DInstance, DParameter>(IContainer<DInstance, DParameter> container, IContainerResolver.IScope scope, Action<IContainerLifecycle, IContainerResolver.IScope> action)
                where DInstance : class
            {
                if (container is IContainerLifecycle lifecycle)
                {
                    action.Invoke(lifecycle, scope);
                }
                return container;
            }
        }
    }
}
