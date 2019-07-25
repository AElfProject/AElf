using System;
using System.Runtime.Serialization;

namespace AElf.OS.Network.Application
{
    public enum NetworkExceptionType { Rpc, PeerUnstable, Unrecoverable  }

    [Serializable]
    public class NetworkException : Exception
    {
        public NetworkExceptionType ExceptionType { get; } 
        
        public NetworkException()
        {
        }

        public NetworkException(string message) : base(message)
        {
        }

        public NetworkException(string message, Exception inner,
            NetworkExceptionType exceptionType = NetworkExceptionType.Rpc) : base(message, inner)
        {
            ExceptionType = exceptionType;
        }

        protected NetworkException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}