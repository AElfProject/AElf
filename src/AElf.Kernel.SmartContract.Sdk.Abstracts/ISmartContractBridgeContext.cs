using System;
using System.Runtime.Serialization;

namespace AElf.Kernel.SmartContract.Sdk
{
    public interface ILimitedSmartContractContext
    {
    }

    [Serializable]
    public class SmartContractBridgeException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SmartContractBridgeException()
        {
        }

        public SmartContractBridgeException(string message) : base(message)
        {
        }

        public SmartContractBridgeException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SmartContractBridgeException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class NoPermissionException : SmartContractBridgeException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public NoPermissionException()
        {
        }

        public NoPermissionException(string message) : base(message)
        {
        }

        public NoPermissionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected NoPermissionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }


    [Serializable]
    public class ContractCallException : SmartContractBridgeException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ContractCallException()
        {
        }

        public ContractCallException(string message) : base(message)
        {
        }

        public ContractCallException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ContractCallException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}