using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Kernel.Node.Protocol;
using AElf.Miner.Miner;
using AElf.SmartContract;
using NLog;

namespace AElf.Kernel.Node
{
    public class MainchainNodeService : INodeService
    {
        private readonly ILogger _logger;
        
        private readonly ITxPoolService _txPoolService;
        private readonly IMiner _miner;
        private readonly IAccountContextService _accountContextService;
        private readonly IBlockVaildationService _blockVaildationService;
        private readonly IChainContextService _chainContextService;
        private readonly IChainService _chainService;
        private readonly IChainCreationService _chainCreationService;
        private readonly IWorldStateDictator _worldStateDictator;
        
        private readonly IBlockSynchronizer _synchronizer;
        private readonly IBlockExecutor _blockExecutor;
        
        public IBlockChain BlockChain { get; }
        private IConsensus _consensus;
        
        private MinerHelper _minerHelper;
        
        public Hash ContractAccountHash =>
            _chainCreationService.GenesisContractHash(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId), SmartContractType.AElfDPoS);

        public int IsMiningInProcess => _minerHelper.IsMiningInProcess;

        public MainchainNodeService(ITxPoolService poolService, 
            IAccountContextService accountContextService,
            IBlockVaildationService blockVaildationService,
            IChainContextService chainContextService,
            IChainCreationService chainCreationService, 
            IWorldStateDictator worldStateDictator,
            IChainService chainService,
            IBlockExecutor blockExecutor,
            IBlockSynchronizer synchronizer,            
            IMiner miner,
            ILogger logger)
        {
            _chainCreationService = chainCreationService;
            _chainService = chainService;
            _worldStateDictator = worldStateDictator;
            //_smartContractService = smartContractService;
            //_functionMetadataService = functionMetadataService;
            _txPoolService = poolService;
            //_transactionManager = txManager;
            _logger = logger;
            _miner = miner;
            _accountContextService = accountContextService;
            _blockVaildationService = blockVaildationService;
            _chainContextService = chainContextService;
            _worldStateDictator = worldStateDictator;
            _blockExecutor = blockExecutor;
            _netManager = netManager;
            _synchronizer = synchronizer;
            _p2p = p2p;
            BlockChain = _chainService.GetBlockChain(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId));
        }
        
        public void Initialize()
        {
            
        }

        public void Start()
        {
            
        }

        public void Stop()
        {
            
        }
    }
}