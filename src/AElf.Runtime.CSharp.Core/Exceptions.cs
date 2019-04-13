using System;

namespace AElf.Runtime.CSharp
{
    public class InvalidMethodNameException : Exception
    {
        public InvalidMethodNameException() : base()
        {
        }

        public InvalidMethodNameException(string message) : base(message)
        {
        }
    }

    public class RuntimeException : Exception
    {
        public RuntimeException() : base()
        {
        }

        public RuntimeException(string message) : base(message)
        {
        }
    }

    public class InvalidCodeException : Exception
    {
        public InvalidCodeException() : base()
        {
        }

        public InvalidCodeException(string message) : base(message)
        {
        }
    }

}