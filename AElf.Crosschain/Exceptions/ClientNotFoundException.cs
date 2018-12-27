using System;

namespace AElf.Crosschain.Exceptions
{
    public class ClientNotFoundException : Exception
    {
        public ClientNotFoundException(string notExistedClient) : base(notExistedClient)
        {
        }
    }
}