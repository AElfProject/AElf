using System;
using System.Runtime.Serialization;

namespace AElf.Sdk.CSharp
{
    [Serializable]
    public class BaseAElfException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public BaseAElfException()
        {
        }

        public BaseAElfException(string message) : base(message)
        {
        }

        public BaseAElfException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BaseAElfException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class AssertionException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public AssertionException()
        {
        }

        public AssertionException(string message) : base(message)
        {
        }

        public AssertionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected AssertionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}