using System;

namespace AElf.CrossChain;

public class CrossChainRequestException : Exception
{
    public CrossChainRequestException(string message) : base(message)
    {
    }

    public CrossChainRequestException(string message, Exception innerException) : base(message,
        innerException)
    {
    }
}