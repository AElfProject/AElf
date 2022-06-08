using System.Collections.Generic;
using AElf.CSharp.CodeOps.Validators;
using AElf.Kernel.CodeCheck;

namespace AElf.CSharp.CodeOps;

public class CSharpCodeCheckException : InvalidCodeException
{
    public CSharpCodeCheckException()
    {
    }

    public CSharpCodeCheckException(string message) : base(message)
    {
    }

    public CSharpCodeCheckException(string message, List<ValidationResult> findings) : base(message)
    {
        Findings = findings;
    }

    public List<ValidationResult> Findings { get; }
}

public class ContractAuditTimeoutException : CSharpCodeCheckException
{
    public ContractAuditTimeoutException()
    {
    }

    public ContractAuditTimeoutException(string message) : base(message)
    {
    }
}

public class MaxInheritanceExceededException : CSharpCodeCheckException
{
    public MaxInheritanceExceededException()
    {
    }

    public MaxInheritanceExceededException(string message) : base(message)
    {
    }
}