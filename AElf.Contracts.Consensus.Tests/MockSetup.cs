using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.ChainController;
using AElf.SmartContract;
using Google.Protobuf;
using ServiceStack;
using AElf.Common;

namespace AElf.Contracts.Consensus.Tests
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MockSetup
    {
        // To differentiate txn identified by From/To/IncrementId
        private static int _incrementId;

        public static ulong NewIncrementId
        {
            get
            {
                var n = Interlocked.Increment(ref _incrementId);
                return (ulong) n;
            }
        }

        public IStateStore StateStore { get; }
        public Hash ChainId { get; } = Hash.LoadByteArray(new byte[] {0x01, 0x02, 0x03});
        private ISmartContractService SmartContractService { get; }

        public readonly IChainContextService ChainContextService;

        private readonly IChainCreationService _chainCreationService;

        public MockSetup(IStateStore stateStore, ISmartContractService smartContractService,
            IChainCreationService chainCreationService, IChainContextService chainContextService)
        {
            StateStore = stateStore;
            _chainCreationService = chainCreationService;
            ChainContextService = chainContextService;
            Task.Factory.StartNew(async () => { await Init(); }).Unwrap().Wait();
            SmartContractService = smartContractService;
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

            await _chainCreationService.CreateNewChainAsync(ChainId,
                new List<SmartContractRegistration> {basicReg, consensusReg, dividendsReg, tokenReg});
        }

        public async Task<IExecutive> GetExecutiveAsync(Address address)
        {
            return await SmartContractService.GetExecutiveAsync(address, ChainId);
        }
    }
}