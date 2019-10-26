using System;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class InitializationFailedException : Exception
    {
        public InitializationFailedException(string message) : base(message)
        {
        }

        public InitializationFailedException(string message, string error) : base($"{message} {error}")
        {
        }
    }
}