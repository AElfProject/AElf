using System;

namespace AElf.Kernel.CodeCheck;

public class CodeCheckJobException : Exception
{
    public CodeCheckJobException()
    {
    }

    public CodeCheckJobException(string message) : base(message)
    {
    }
}