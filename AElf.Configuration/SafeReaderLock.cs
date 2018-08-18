using System.Threading;

namespace AElf.Configuration
{
    public class SafeReaderLock : ISafeLock
    {
        private readonly ReaderWriterLockSlim _locker;

        public SafeReaderLock(ReaderWriterLockSlim locker)
        {
            this._locker = locker;
        }
        
        public void Dispose()
        {
            _locker.ExitReadLock();
        }

        public void EnterLock()
        {
            _locker.EnterReadLock();
        }
    }
}