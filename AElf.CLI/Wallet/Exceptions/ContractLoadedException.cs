using System;

namespace AElf.CLI.Wallet.Exceptions
{
    public class ContractLoadedException : Exception
    {
        public ContractLoadedException() : base("Contract loading failed")
        {
            
        }
    }
}