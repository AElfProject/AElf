using System.Collections.Generic;
using AElf.Standards.ACS0;
using AElf.Kernel.SmartContract.Application;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Contracts.TestBase
{
    public static class GenesisSmartContractDtoExtensions
    {
        public static void AddGenesisSmartContract(this List<GenesisSmartContractDto> genesisSmartContracts,
            byte[] code, Hash name = null,
            List<ContractInitializationMethodCall> contractInitializationMethodCallList = null)
        {
            var genesisSmartContractDto = new GenesisSmartContractDto
            {
                Code = code,
                SystemSmartContractName = name,
                ContractInitializationMethodCallList = new List<ContractInitializationMethodCall>()
            };
            genesisSmartContracts.Add(genesisSmartContractDto);

            if (contractInitializationMethodCallList == null)
                return;
            genesisSmartContractDto.ContractInitializationMethodCallList = new List<ContractInitializationMethodCall>();
            foreach (var contractInitializationMethodCall in contractInitializationMethodCallList)
            {
                genesisSmartContractDto.AddGenesisTransactionMethodCall(contractInitializationMethodCall);
            }
        }
    }
}