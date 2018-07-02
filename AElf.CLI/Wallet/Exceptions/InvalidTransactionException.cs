using System;

namespace AElf.CLI.Wallet.Exceptions
{
    public class InvalidTransactionException : Exception
    {
        public InvalidTransactionException() : base("Invalid transaction data.")
        {
            
        }
    }
}