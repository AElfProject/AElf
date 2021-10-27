using System;
using System.Runtime.Serialization;

namespace AElf.Kernel.SmartContract.Application
{
    [Serializable]
    public class SmartContractFindRegistrationException: Exception
    {
        public SmartContractFindRegistrationException()
        {
        }

        public SmartContractFindRegistrationException(string message) : base(message)
        {
        }

        public SmartContractFindRegistrationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SmartContractFindRegistrationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}