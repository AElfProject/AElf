using System;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class BlockMiningException : Exception
    {
        public BlockMiningException(string message) : base(message)
        {
        }
        
        public BlockMiningException(string message, Exception e) : base(message, e)
        {
        }
    }
}