using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Kernel.Managers;
using AElf.ChainController;
using AElf.SmartContract;
using Google.Protobuf;
using ServiceStack;
using AElf.Common;
using AElf.Execution.Execution;

namespace AElf.Contracts.Consensus.Tests
{
    public class MockSetup
    {
        // To differentiate txn identified by From/To/IncrementId
        private static int _incrementId;

        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) n;
        }

        public IStateStore StateStore { get; }
        public Hash ChainId { get; } = Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 });
        public ISmartContractManager SmartContractManager;
        public ISmartContractService SmartContractService { get; }
        private IFunctionMetadataService _functionMetadataService;

        public Address ConsensusContractAddress { get; private set; }
        public Address DividendsContractAddress { get; private set; }

        public IChainContextService ChainContextService;

        public ServicePack ServicePack;

        private IChainCreationService _chainCreationService;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;

        public MockSetup(IStateStore stateStore, ISmartContractService smartContractService,
            IChainCreationService chainCreationService, IChainContextService chainContextService,
            IFunctionMetadataService functionMetadataService, ISmartContractRunnerFactory smartContractRunnerFactory)
        {
            StateStore = stateStore;
            _chainCreationService = chainCreationService;
            ChainContextService = chainContextService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            Task.Factory.StartNew(async () => { await Init(); }).Unwrap().Wait();
            SmartContractService = smartContractService;

            ServicePack = new ServicePack
            {
                ChainContextService = chainContextService,
                SmartContractService = SmartContractService,
                ResourceDetectionService = null,
                StateStore = StateStore
            };
        }

        public string ConsensusContractName => "AElf.Contracts.Consensus";
        public string DividendsContractName => "AElf.Contracts.Dividends";
        public string TokenContractName => "AElf.Contracts.Token";
        public string ZeroContractName => "AElf.Contracts.Genesis";

        public byte[] GetContractCode(string contractName)
        {
            byte[] code;
            using (var file =
                File.OpenRead(
                    Path.GetFullPath($"../../../../{contractName}/bin/Debug/netstandard2.0/{contractName}.dll")))
            {
                code = file.ReadFully();
            }

            return code;
        }

        private async Task Init()
        {
            var consensusReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(GetContractCode(ConsensusContractName)),
                ContractHash = Hash.FromRawBytes(GetContractCode(ConsensusContractName)),
                SerialNumber = GlobalConfig.ConsensusContract
            };
            var dividendsReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(GetContractCode(DividendsContractName)),
                ContractHash = Hash.FromRawBytes(GetContractCode(DividendsContractName)),
                SerialNumber = GlobalConfig.DividendsContract
            };
            var tokenReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(GetContractCode(TokenContractName)),
                ContractHash = Hash.FromRawBytes(GetContractCode(TokenContractName)),
                SerialNumber = GlobalConfig.TokenContract
            };
            var basicReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(GetContractCode(ZeroContractName)),
                ContractHash = Hash.FromRawBytes(GetContractCode(ZeroContractName)),
                SerialNumber = GlobalConfig.GenesisBasicContract
            };

            var chain1 =
                await _chainCreationService.CreateNewChainAsync(ChainId,
                    new List<SmartContractRegistration> {basicReg, consensusReg, dividendsReg, tokenReg});
        }

        public async Task<IExecutive> GetExecutiveAsync(Address address)
        {
            return await SmartContractService.GetExecutiveAsync(address, ChainId);
        }
    }
}