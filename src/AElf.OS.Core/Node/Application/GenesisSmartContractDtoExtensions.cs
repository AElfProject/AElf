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
            Type smartContractType, Hash name = null,
            SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList = null)
        {
            genesisSmartContracts.Add(new GenesisSmartContractDto()
            {
                Code = File.ReadAllBytes(smartContractType.Assembly.Location),
                SystemSmartContractName = name,
                TransactionMethodCallList = systemTransactionMethodCallList
            });
        }
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
//        public static void AddGenesisSmartContract<T>(this List<GenesisSmartContractDto> genesisSmartContracts,
//            Hash name = null, SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList = null)
//        {
//            // TODO: Change this
//            genesisSmartContracts.AddGenesisSmartContract(typeof(T), name, systemTransactionMethodCallList);
//        }
        public static void Add(this SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList, string methodName,
            IMessage input)
        {
            systemTransactionMethodCallList.Value.Add(new SystemContractDeploymentInput.Types.SystemTransactionMethodCall()
            {
                MethodName = methodName,
                Params = input?.ToByteString() ?? ByteString.Empty
            });
        }

        public static void Add(this SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList, string methodName,
            ByteString input)
        {
            systemTransactionMethodCallList.Value.Add(
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCall()
                {
                    MethodName = methodName,
                    Params = input ?? ByteString.Empty
                });
        }

        public static void AddGenesisSmartContract(this List<GenesisSmartContractDto> genesisSmartContracts,
            byte[] code, Hash name, Action<SystemContractDeploymentInput.Types.SystemTransactionMethodCallList> action)
        {
            SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();

            action?.Invoke(systemTransactionMethodCallList);

            genesisSmartContracts.AddGenesisSmartContract(code, name, systemTransactionMethodCallList);
        }

//        public static void AddGenesisSmartContract<T>(this List<GenesisSmartContractDto> genesisSmartContracts,
//            Hash name, Action<SystemContractDeploymentInput.Types.SystemTransactionMethodCallList> action)
//        {
//            SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
//
//            action?.Invoke(systemTransactionMethodCallList);
//
//            genesisSmartContracts.AddGenesisSmartContract<T>(name, systemTransactionMethodCallList);
//        }

    }
}