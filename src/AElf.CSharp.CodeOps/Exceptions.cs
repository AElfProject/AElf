using System;
using System.Collections.Generic;
using AElf.CSharp.CodeOps.Validators;

namespace AElf.CSharp.CodeOps
{
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
    
    public class ContractAuditTimeoutException : Exception
    {
        public ContractAuditTimeoutException() : base()
        {
        }

        public ContractAuditTimeoutException(string message) : base(message)
        {
        }
    }
    
    public class MaxInheritanceExceededException : Exception
    {
        public MaxInheritanceExceededException() : base()
        {
        }

        public MaxInheritanceExceededException(string message) : base(message)
        {
        }
    }
}