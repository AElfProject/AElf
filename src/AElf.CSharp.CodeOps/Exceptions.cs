using System.Collections.Generic;
using AElf.CSharp.CodeOps.Validators;
using AElf.Kernel.CodeCheck;

namespace AElf.CSharp.CodeOps
{
    public class CSharpInvalidCodeException : InvalidCodeException
    {
        public List<ValidationResult> Findings { get; }

        public CSharpInvalidCodeException() : base()
        {
        }

        public CSharpInvalidCodeException(string message) : base(message)
        {
        }

        public CSharpInvalidCodeException(string message, List<ValidationResult> findings) : base(message)
        {
            Findings = findings;
        }
    }
}