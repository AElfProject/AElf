using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Acs0;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractAddressService
    {
        Task<Address> GetAddressByContractNameAsync(IChainContext chainContext, string name);
        Task<SmartContractAddressDto> GetSmartContractAddressAsync(IChainContext chainContext, string name);
        Task SetSmartContractAddressAsync(IBlockIndex blockIndex, string name, Address address);

        Address GetZeroSmartContractAddress();

        Address GetZeroSmartContractAddress(int chainId);

        Task<IReadOnlyDictionary<Hash, Address>> GetSystemContractNameToAddressMappingAsync(IChainContext chainContext);
    }

    public class SmartContractAddressService : ISmartContractAddressService, ISingletonDependency
    {
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressProvider _smartContractAddressProvider;
        private readonly IEnumerable<ISmartContractAddressNameProvider> _smartContractAddressNameProviders;
        private readonly IBlockchainService _blockchainService;

        public SmartContractAddressService(IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider, 
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService, 
            ISmartContractAddressProvider smartContractAddressProvider,
            IEnumerable<ISmartContractAddressNameProvider> smartContractAddressNameProviders, 
            IBlockchainService blockchainService)
        {
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressProvider = smartContractAddressProvider;
            _smartContractAddressNameProviders = smartContractAddressNameProviders;
            _blockchainService = blockchainService;
        }

        public async Task<Address> GetAddressByContractNameAsync(IChainContext chainContext, string name)
        {
            var smartContractAddress = await _smartContractAddressProvider.GetSmartContractAddressAsync(chainContext, name);
            var address = smartContractAddress?.Address;
            if (address == null) address = await GetSmartContractAddressFromStateAsync(chainContext, name);
            return address;
        }

        public async Task<SmartContractAddressDto> GetSmartContractAddressAsync(IChainContext chainContext, string name)
        {
            var smartContractAddress =
                await _smartContractAddressProvider.GetSmartContractAddressAsync(chainContext, name);
            if (smartContractAddress != null)
            {
                var smartContractAddressDto = new SmartContractAddressDto
                {
                    SmartContractAddress = smartContractAddress,
                    Irreversible = await CheckSmartContractAddressIrreversibleAsync(smartContractAddress)
                };

                return smartContractAddressDto;
            }

            var address = await GetSmartContractAddressFromStateAsync(chainContext, name);
            if (address == null) return null;
            return new SmartContractAddressDto
            {
                SmartContractAddress = new SmartContractAddress
                {
                    Address = address
                }
            };
        }

        private async Task<bool> CheckSmartContractAddressIrreversibleAsync(SmartContractAddress smartContractAddress)
        {
            var chain = await _blockchainService.GetChainAsync();
            if (smartContractAddress.BlockHeight > chain.LastIrreversibleBlockHeight) return false;

            var blockHash = await _blockchainService.GetBlockHashByHeightAsync(chain,
                smartContractAddress.BlockHeight, chain.LastIrreversibleBlockHash);
            return blockHash == smartContractAddress.BlockHash;
        }

        public virtual async Task SetSmartContractAddressAsync(IBlockIndex blockIndex, string name, Address address)
        {
            await _smartContractAddressProvider.SetSmartContractAddressAsync(blockIndex, name, address);
        }

        public Address GetZeroSmartContractAddress()
        {
            return _defaultContractZeroCodeProvider.ContractZeroAddress;
        }

        public Address GetZeroSmartContractAddress(int chainId)
        {
            return _defaultContractZeroCodeProvider.GetZeroSmartContractAddress(chainId);
        }

        public virtual async Task<IReadOnlyDictionary<Hash, Address>> GetSystemContractNameToAddressMappingAsync(
            IChainContext chainContext)
        {
            var map = new Dictionary<Hash,Address>();
            foreach (var smartContractAddressNameProvider in _smartContractAddressNameProviders)
            {
                var address =
                    await GetAddressByContractNameAsync(chainContext, smartContractAddressNameProvider.ContractStringName);
                if(address != null)
                    map[smartContractAddressNameProvider.ContractName] = address;
            }
            return new ReadOnlyDictionary<Hash, Address>(map);
        }

        private async Task<Address> GetSmartContractAddressFromStateAsync(IChainContext chainContext, string name)
        {
            var zeroAddress = _defaultContractZeroCodeProvider.ContractZeroAddress;
            var tx = new Transaction
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(ACS0Container.ACS0Stub.GetContractAddressByName),
                Params = Hash.LoadFromBase64(name).ToByteString()
            };
            var address = await _transactionReadOnlyExecutionService.ExecuteAsync<Address>(
                chainContext, tx, TimestampHelper.GetUtcNow(), false);

            return address == null || address.Value.IsEmpty ? null : address;
        }
    }
    
    public class SmartContractAddressDto
    {
        public SmartContractAddress SmartContractAddress { get; set; }
        public bool Irreversible { get; set; }
    }
}