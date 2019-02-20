//using System.Collections.Generic;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
//using AElf.Kernel;
//using AElf.ChainController;
//using AElf.SmartContract;
//using Google.Protobuf;
//using AElf.Common;
//using AElf.Kernel.SmartContractExecution.Domain;
//using Volo.Abp.DependencyInjection;
//
//namespace AElf.Contracts.Consensus.Tests
//{
//    // ReSharper disable ClassNeverInstantiated.Global
//    public class MockSetup : ITransientDependency
//    {
//        // To differentiate txn identified by From/To/IncrementId
//        private static int _incrementId;
//
//        public static ulong NewIncrementId
//        {
//            get
//            {
//                var n = Interlocked.Increment(ref _incrementId);
//                return (ulong) n;
//            }
//        }
//
//        public IStateManager StateManager { get; }
//        
//        public int ChainId { get; } = ChainHelpers.GetChainId(123);
//        
//        private ISmartContractService SmartContractService { get; }
//
//        private readonly IChainCreationService _chainCreationService;
//
//        public MockSetup(IStateManager stateManager, ISmartContractService smartContractService,
//            IChainCreationService chainCreationService)
//        {
//            StateManager = stateManager;
//            SmartContractService = smartContractService;
//
//            _chainCreationService = chainCreationService;
//            Task.Factory.StartNew(async () => { await Initialize(); }).Unwrap().Wait();
//        }
//
//        public string ConsensusContractName => "AElf.Contracts.Consensus";
//        public string DividendsContractName => "AElf.Contracts.Dividends";
//        public string TokenContractName => "AElf.Contracts.Token";
//        public string ZeroContractName => "AElf.Contracts.Genesis";
//
//        public static byte[] GetContractCode(string contractName)
//        {
//            return File.ReadAllBytes(Path.GetFullPath($"../../../../{contractName}/bin/Debug/netstandard2.0/{contractName}.dll"));
//        }
//
//        private async Task Initialize()
//        {
//            var consensusReg = new SmartContractRegistration
//            {
//                Category = 0,
//                ContractBytes = ByteString.CopyFrom(GetContractCode(ConsensusContractName)),
//                ContractHash = Hash.FromRawBytes(GetContractCode(ConsensusContractName)),
//                SerialNumber = GlobalConfig.ConsensusContract
//            };
//            var dividendsReg = new SmartContractRegistration
//            {
//                Category = 0,
//                ContractBytes = ByteString.CopyFrom(GetContractCode(DividendsContractName)),
//                ContractHash = Hash.FromRawBytes(GetContractCode(DividendsContractName)),
//                SerialNumber = GlobalConfig.DividendsContract
//            };
//            var tokenReg = new SmartContractRegistration
//            {
//                Category = 0,
//                ContractBytes = ByteString.CopyFrom(GetContractCode(TokenContractName)),
//                ContractHash = Hash.FromRawBytes(GetContractCode(TokenContractName)),
//                SerialNumber = GlobalConfig.TokenContract
//            };
//            var basicReg = new SmartContractRegistration
//            {
//                Category = 0,
//                ContractBytes = ByteString.CopyFrom(GetContractCode(ZeroContractName)),
//                ContractHash = Hash.FromRawBytes(GetContractCode(ZeroContractName)),
//                SerialNumber = GlobalConfig.GenesisBasicContract
//            };
//
//            await _chainCreationService.CreateNewChainAsync(ChainId,
//                new List<SmartContractRegistration> {basicReg, consensusReg, dividendsReg, tokenReg});
//        }
//    }
//}