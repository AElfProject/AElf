using System;

namespace AElf.CrossChain.Grpc.Exceptions
{
    public class ClientNotFoundException : Exception
    {
        public ClientNotFoundException(string notExistedClient) : base(notExistedClient)
        {
        }
    }
}