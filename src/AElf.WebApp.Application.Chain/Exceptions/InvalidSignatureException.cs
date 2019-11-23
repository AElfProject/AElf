using System;

namespace AElf.WebApp.Application.Chain
{
    public class InvalidSignatureException : Exception
    {
        public InvalidSignatureException(string message) : base(message)
        {
        }
    }
}