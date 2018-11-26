using System;

namespace AElf.Miner.Rpc.Exceptions
{
    public class ClientNotFoundException : Exception
    {
        public ClientNotFoundException(string notExistedClient) : base(notExistedClient)
        {
        }
    }
}