using System;

namespace AElf.Configuration
{
    public interface ISafeLock: IDisposable
    {
        void EnterLock();
    }
}