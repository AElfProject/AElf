using System.Threading.Tasks;
using AElf.ChainController;
using NLog;

namespace AElf.Kernel.Node
{
    public class PoTC
    {
        public MainChainNode Node { get; }
        public ulong ConsensusMemory { get; set; }
        private ILogger _logger;
        private ITxPoolService _txPoolService;
        private IMiner _miner;
        private IP2P _p2p;

        public PoTC(ILogger logger, MainChainNode node, IMiner miner,
            IAccountContextService accountContextService,
            ITxPoolService txPoolService,
            IP2P p2p)
        {
            _logger = logger;
            _miner = miner;
            _p2p = p2p;
            _txPoolService = txPoolService;
            Node = node;
        }

        // ReSharper disable once InconsistentNaming
        public async Task PoTCProcess()
        {
            while (true)
            {
                var count = await _txPoolService.GetPoolSize();
                if (ConsensusMemory != count)
                {
                    _logger?.Trace($"Current tx pool size: {count} / {Globals.ExpectedTransanctionCount}");
                    ConsensusMemory = count;
                }

                if (count >= Globals.ExpectedTransanctionCount)
                {
                    _logger?.Trace("Will produce one block.");
                    var block = await _miner.Mine();
                    await _p2p.BroadcastBlock(block);
                }
            }
        }
    }
}