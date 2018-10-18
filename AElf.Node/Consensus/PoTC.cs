using System.Threading.Tasks;
using AElf.ChainController.TxMemPool;
using AElf.Miner.Miner;
using NLog;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    // ReSharper disable InconsistentNaming
    public class PoTC : IConsensus
    {
        private ulong ConsensusMemory { get; set; }
        private readonly ILogger _logger;
        private readonly ITxPoolService _txPoolService;
        private readonly IMiner _miner;

        public PoTC(IMiner miner, ITxPoolService txPoolService)
        {
            _miner = miner;
            _txPoolService = txPoolService;

            _logger = LogManager.GetLogger(nameof(PoTC));
        }

        public async Task Start()
        {
            //await Node.Mine();
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }

        public void Hang()
        {
            throw new System.NotImplementedException();
        }

        public void Recover()
        {
            throw new System.NotImplementedException();
        }

        // ReSharper disable once InconsistentNaming
        public async Task Update()
        {
            while (true)
            {
                var count = await _txPoolService.GetPoolSize();
                if (ConsensusMemory != count)
                {
                    _logger?.Trace($"Current tx pool size: {count} / {GlobalConfig.ExpectedTransactionCount}");
                    ConsensusMemory = count;
                }

                if (count >= GlobalConfig.ExpectedTransactionCount)
                {
                    _logger?.Trace("Will produce one block.");
                    await _miner.Mine();
                }
            }
        }

        public async Task RecoverMining()
        {
            await Task.CompletedTask;
        }

        public bool IsAlive()
        {
            return true;
        }
    }
}