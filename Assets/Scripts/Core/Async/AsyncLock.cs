using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace JigsawPrototype.Core.Async
{
    /// <summary>
    /// Minimal async lock for UniTask workflows
    /// Intended for Unity main-thread flows
    /// </summary>
    public sealed class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public async UniTask<IDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            return new Releaser(_semaphore);
        }

        private readonly struct Releaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public Releaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}

