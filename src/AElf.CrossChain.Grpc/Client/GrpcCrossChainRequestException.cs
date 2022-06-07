using System;

namespace AElf.CrossChain.Grpc.Client;

public class GrpcCrossChainRequestException : CrossChainRequestException
{
    public GrpcCrossChainRequestException(string message) : base(message)
    {
    }

    public GrpcCrossChainRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}