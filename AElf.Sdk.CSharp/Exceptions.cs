using System;
namespace AElf.Sdk.CSharp
{
    public class BaseAElfException : Exception
    {
        public BaseAElfException(string message) : base(message) { }
    }
    public class AssertionError : BaseAElfException
    {
        public AssertionError(string message) : base(message) { }
    }
    public class InternalError : BaseAElfException
    {
        public InternalError(string message) : base(message) { }
    }
}
