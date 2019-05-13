using System;
using System.Collections.Generic;
using AElf.Runtime.CSharp.Validators;

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
        public List<ValidationResult> Findings { get; }
        public InvalidCodeException() : base()
        {
        }

        public InvalidCodeException(string message) : base(message)
        {
        }
        
        public InvalidCodeException(string message, List<ValidationResult> findings) : base(message)
        {
            Findings = findings;
        }
    }
}
