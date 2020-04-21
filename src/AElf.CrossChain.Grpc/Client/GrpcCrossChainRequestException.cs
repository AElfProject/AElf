using System;
using AElf.CrossChain.Communication;

namespace AElf.CrossChain.Grpc.Client
{
    public class GrpcCrossChainRequestException : CrossChainRequestException
    {
        public GrpcCrossChainRequestException(string message) : base(message)
        {
        }

        public GrpcCrossChainRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}