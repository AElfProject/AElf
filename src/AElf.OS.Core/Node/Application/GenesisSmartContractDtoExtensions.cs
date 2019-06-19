using System;
using System.Collections.Generic;
using System.IO;
using Acs0;
using AElf.Types;
using Google.Protobuf;

namespace AElf.OS.Node.Application
{
    public static class GenesisSmartContractDtoExtensions
    {
        public static void AddGenesisSmartContract(this List<GenesisSmartContractDto> genesisSmartContracts,
            byte[] code, Hash name = null,
            SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList = null)
        {
            genesisSmartContracts.Add(new GenesisSmartContractDto()
            {
                Code = code,
                SystemSmartContractName = name,
                TransactionMethodCallList = systemTransactionMethodCallList
            });
        }
        
        public static void Add(this SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList, string methodName,
            IMessage input)
        {
            systemTransactionMethodCallList.Value.Add(new SystemContractDeploymentInput.Types.SystemTransactionMethodCall()
            {
                MethodName = methodName,
                Params = input?.ToByteString() ?? ByteString.Empty
            });
        }

    }
}