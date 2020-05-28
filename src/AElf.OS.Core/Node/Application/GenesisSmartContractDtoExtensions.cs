using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;

namespace AElf.OS.Node.Application
{
    public static class GenesisSmartContractDtoExtensions
    {
        public static void AddGenesisTransactionMethodCall(this GenesisSmartContractDto genesisSmartContractDto,
            params ContractInitializationMethodCall[] contractInitializationMethodCalls)
        {
            genesisSmartContractDto.ContractInitializationMethodCallList.AddRange(contractInitializationMethodCalls);
        }
        
        public static void Add(
            this List<ContractInitializationMethodCall> contractInitializationMethodCallList,
            string methodName,
            IMessage input)
        {
            contractInitializationMethodCallList.Add(new ContractInitializationMethodCall
            {
                MethodName = methodName,
                Params = input?.ToByteString() ?? ByteString.Empty
            });
        }
    }
}