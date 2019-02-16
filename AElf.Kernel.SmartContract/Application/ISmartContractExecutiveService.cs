using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractExecutiveService
    {
        
        Task<IExecutive> GetExecutiveAsync(int chainId, Hash contractHash);
        Task PutExecutiveAsync(int chainId, Address account, IExecutive executive);
//
//
//
//        Task<IMessage> GetAbiAsync(int chainId, Address account);
//
//        Task<SmartContractRegistration> GetContractByAddressAsync(int chainId, Address address);
    }
    
    
    public class SmartContractExecutiveService: ISmartContractExecutiveService
    {
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;

        
        private readonly ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>> _executivePools =
            new ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>();
        
//        private async Task<Hash> GetContractHashAsync(int chainId, Address address)
//        {
//            Hash contractHash;
//            var zeroContractAddress = ContractHelpers.GetGenesisBasicContractAddress(chainId);
//
//            if (address == zeroContractAddress)
//            {
//                contractHash = Hash.FromMessage(zeroContractAddress);
//            }
//            else
//            {
//                var result = await CallContractAsync(true, chainId, zeroContractAddress, "GetContractHash", address);
//
//                contractHash = result.DeserializeToPbMessage<Hash>();
//            }
//
//            return contractHash;
//        }
        
//        public async Task<SmartContractRegistration> GetContractByAddressAsync(int chainId, Address address)
//        {
//            var contractHash = await GetContractHashAsync(chainId, address);
//            return await _smartContractManager.GetAsync(contractHash);
//        }
        private async Task<ConcurrentBag<IExecutive>> GetPoolForAsync(Hash contractHash)
        {
            if (!_executivePools.TryGetValue(contractHash, out var pool))
            {
                pool = new ConcurrentBag<IExecutive>();
                _executivePools[contractHash] = pool;
            }

            return pool;
        }
        public async Task<IExecutive> GetExecutiveAsync(int chainId, Hash contractHash,)
        {
            var pool = await GetPoolForAsync(contractHash);
            if (!pool.TryTake(out var executive))
            {
                // get registration
                var reg = await GetContractByAddressAsync(chainId, contractAddress);
                // get runner
                var runner = _smartContractRunnerContainer.GetRunner(reg.Category);
                if (runner == null)
                {
                    throw new NotSupportedException($"Runner for category {reg.Category} is not registered.");
                }
                // run smartcontract executive info and return executive
                executive = await runner.RunAsync(reg);
                executive.ContractHash = reg.ContractHash;
            }
            executive.SetStateProviderFactory(_stateProviderFactory);

            executive.SetSmartContractContext(new SmartContractContext()
            {
                ChainId = chainId,
                ContractAddress = contractAddress,
                SmartContractService = this,
                ChainService = _chainService
            });

            return executive;
        }

        public async Task PutExecutiveAsync(int chainId, Address account, IExecutive executive)
        {
            executive.SetTransactionContext(new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    To = account // This is to ensure that the contract has same address
                }
            });
            executive.SetDataCache(new Dictionary<StatePath, StateCache>());
            (await GetPoolForAsync(chainId, account)).Add(executive);

            await Task.CompletedTask;
        }
    }
}