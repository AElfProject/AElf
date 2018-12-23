using System;

namespace AElf.Synchronization.BlockSynchronization
{
    public class UnlinkableBlockException : Exception
    {
        public UnlinkableBlockException() : base("Block unlinkable")
        {
        }
    }
}