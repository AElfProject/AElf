using System;

namespace AElf.Runtime.CSharp.Core.ABI
{
    public class InvalidInputException : Exception
    {
        public InvalidInputException(string message) : base(message)
        {
        }
    }
}