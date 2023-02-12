using System;

namespace AElf.Runtime.CSharp.Core;

public class InvalidMethodNameException : Exception
{
    public InvalidMethodNameException()
    {
    }

    public InvalidMethodNameException(string message) : base(message)
    {
    }
}

public class RuntimeException : Exception
{
    public RuntimeException()
    {
    }

    public RuntimeException(string message) : base(message)
    {
    }
}