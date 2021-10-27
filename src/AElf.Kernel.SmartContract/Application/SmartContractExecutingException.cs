using System;
using System.Runtime.Serialization;

namespace AElf.Kernel.SmartContract.Application
{
    [Serializable]
    public class SmartContractExecutingException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SmartContractExecutingException()
        {
        }

        public SmartContractExecutingException(string message) : base(message)
        {
        }

        public SmartContractExecutingException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SmartContractExecutingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}