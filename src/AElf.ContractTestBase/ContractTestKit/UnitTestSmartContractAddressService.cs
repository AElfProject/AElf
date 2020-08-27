using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.ContractTestBase.ContractTestKit
{
    public class UnitTestSmartContractAddressService : SmartContractAddressService
    {
        private readonly ConcurrentDictionary<Hash, Address> _hashToAddressMap =
            new ConcurrentDictionary<Hash, Address>();
        
        public UnitTestSmartContractAddressService(IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressProvider smartContractAddressProvider,
            IEnumerable<ISmartContractAddressNameProvider> smartContractAddressNameProviders,
            IBlockchainService blockchainService) : base(defaultContractZeroCodeProvider,
            transactionReadOnlyExecutionService, smartContractAddressProvider, smartContractAddressNameProviders,
            blockchainService)
        {
        }
    
        public override async Task<IReadOnlyDictionary<Hash, Address>> GetSystemContractNameToAddressMappingAsync(
            IChainContext chainContext)
        {
            var baseDictionary = await base.GetSystemContractNameToAddressMappingAsync(chainContext);
            var dictionary = new Dictionary<Hash, Address>();
            foreach (var keyValuePair in baseDictionary.Concat(_hashToAddressMap))
            {
                dictionary.TryAdd(keyValuePair.Key, keyValuePair.Value);
            } 
            return new ReadOnlyDictionary<Hash, Address>(dictionary);
        }
    
        public override Task SetSmartContractAddressAsync(IBlockIndex blockIndex, string contractName, Address address)
        {
            _hashToAddressMap[Hash.LoadFromBase64(contractName)] = address;
            return base.SetSmartContractAddressAsync(blockIndex, contractName, address);
        }
    }
}