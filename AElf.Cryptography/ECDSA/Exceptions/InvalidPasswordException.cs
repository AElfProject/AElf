using System;

namespace AElf.Cryptography.ECDSA.Exceptions
{
    public class InvalidPasswordException : Exception
    {
        public InvalidPasswordException(string msg, Exception e) : base(msg, e)
        {
        }
    }
}