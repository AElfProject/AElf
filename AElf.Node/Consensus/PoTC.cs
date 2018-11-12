using System.Threading.Tasks;
using AElf.Miner.Miner;
using NLog;
using AElf.Common;
using AElf.Miner.TxMemPool;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    // ReSharper disable InconsistentNaming
    public class PoTC : IConsensus
    {
        private ulong ConsensusMemory { get; set; }
        private readonly ILogger _logger;
        private readonly ITxHub _txHub;
        private readonly IMiner _miner;

        public PoTC(IMiner miner, ITxHub txHub)
        {
            _miner = miner;
            _txHub = txHub;

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
                var count = (ulong)(await _txHub.GetReceiptsOfExecutablesAsync()).Count;
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
        
        public bool Shutdown()
        {
            return false;
        }
    }
}