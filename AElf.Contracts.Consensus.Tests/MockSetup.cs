using System;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Domain;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.Consensus.Tests
{
    // ReSharper disable ClassNeverInstantiated.Global
    public class MockSetup : ITransientDependency
    {
        public IBlockchainStateManager StateManager { get; }
        
        public int ChainId { get; } = ChainHelpers.GetChainId(123);
        
        private ISmartContractService SmartContractService { get; }

        private readonly IChainCreationService _chainCreationService;

        public MockSetup(IBlockchainStateManager stateManager, ISmartContractService smartContractService,
            IChainCreationService chainCreationService)
        {
            StateManager = stateManager;
            SmartContractService = smartContractService;

            _chainCreationService = chainCreationService;
            Task.Factory.StartNew(async () => { await Initialize(); }).Unwrap().Wait();
        }

        public string ConsensusContractName => "AElf.Contracts.Consensus.DPoS";
        public string DividendsContractName => "AElf.Contracts.Dividends";
        public string TokenContractName => "AElf.Contracts.Token2";
        public string ZeroContractName => "AElf.Contracts.Genesis2";

        public static byte[] GetContractCode(string contractName)
        {
            return File.ReadAllBytes(Path.GetFullPath($"../../../../{contractName}/bin/Debug/netstandard2.0/{contractName}.dll"));
        }

        private async Task Initialize()
        {
            throw new NotImplementedException();
        }
    }
}