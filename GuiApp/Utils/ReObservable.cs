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
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Utils
{
    public static class ReObservable
    {
        public interface IReObservable<T> : IObservable<T>
        {
            IObservable<T> WithExceptionHandler(Action<Exception> handler);
        }

        public static IReObservable<R> Create<S, R>(Func<IObservable<S>> source, Func<S, CancellationToken, Task<R>> selector)
        {
            return new ReObservableImpl<S, R>(source, selector);
        }

        public static IReObservable<R> Observe<S, R>(this Func<IObservable<S>> source, Func<S, CancellationToken, Task<R>> selector)
        {
            return Create(source, selector);
        }

        private class ReObservableImpl<S, R> : IReObservable<R>
        {
            private readonly Func<IObservable<S>> source;
            private readonly Func<S, CancellationToken, Task<R>> selector;
            private readonly Action<Exception>? exceptionHandler;

            internal ReObservableImpl(Func<IObservable<S>> source, Func<S, CancellationToken, Task<R>> selector, Action<Exception>? exceptionHandler = null)
            {
                this.source = source;
                this.selector = selector;
                this.exceptionHandler = exceptionHandler;
            }

            public IDisposable Subscribe(IObserver<R> observer)
            {
                return Subscribe(new(), observer, false);
            }

            public IObservable<R> WithExceptionHandler(Action<Exception> handler)
            {
                return new ReObservableImpl<S, R>(source, selector, handler);
            }

            private IDisposable Subscribe(CompositeDisposable disposables, IObserver<R> observer, bool skip)
            {
                IObservable<S> obs = source.Invoke();
                lock (disposables)
                {
                    disposables.Add(obs
                        .Catch<S, Exception>(ex =>
                        {
                            observer.OnError(ex);
                            return Observable.Return(default(S)!);
                        })
                        .Skip(skip ? 1 : 0)
                        .SelectMany(selector)
                        .Catch<R, Exception>(ex =>
                        {
                            lock (disposables)
                            {
                                var disposable = disposables.FirstOrDefault((IDisposable)null!);
                                if (disposable != null) disposables.Remove(disposable);
                                try
                                {
                                    Subscribe(disposables, observer, true);
                                }
                                finally
                                {
                                    disposable?.Dispose();
                                }
                            }
                            exceptionHandler?.Invoke(ex);
                            return default!;
                        })
                        .Finally(observer.OnCompleted)
                        .Subscribe(observer.OnNext));
                }
                return disposables;
            }
        }
    }
}
