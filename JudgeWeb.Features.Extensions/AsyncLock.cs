namespace System.Threading.Tasks
{
    public sealed class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim semaphore;
        private readonly Releaser releaser;

        public AsyncLock()
        {
            semaphore = new SemaphoreSlim(1);
            releaser = new Releaser(this);
        }

        public async Task<IDisposable> LockAsync()
        {
            await semaphore.WaitAsync();
            return releaser;
        }

        private struct Releaser : IDisposable
        {
            private readonly AsyncLock @lock;

            public Releaser(AsyncLock asyncLock)
            {
                @lock = asyncLock;
            }

            public void Dispose()
            {
                if (@lock != null)
                {
                    @lock.semaphore.Release();
                }
            }
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }
}
