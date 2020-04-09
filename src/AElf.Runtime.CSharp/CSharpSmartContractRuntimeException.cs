using System;

namespace AElf.Runtime.CSharp
{
    public class InvalidAssemblyException : Exception
    {
        public InvalidAssemblyException()
        {
        }

        public InvalidAssemblyException(string message) : base(message)
        {
        }
    }
}