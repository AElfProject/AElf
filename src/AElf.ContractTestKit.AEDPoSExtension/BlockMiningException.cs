using System;

namespace AElf.ContractTestKit.AEDPoSExtension
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