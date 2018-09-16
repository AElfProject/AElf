using System;
using System.Threading.Tasks;
using AElf.Kernel.Consensus;
using NLog;

namespace AElf.Kernel.Node
{
    public class StandaloneNodeConsensusPlaceHolder : IConsensus
    {
        public IDisposable ConsensusDisposable { get; set; }
        private readonly ILogger _logger;
        private readonly IP2P _p2p;
        private SingleNodeTestObserver SingleNodeTestObserver => new SingleNodeTestObserver(_logger, SingleNodeMining);
        
        public StandaloneNodeConsensusPlaceHolder(ILogger logger, IP2P p2p)
        {
            _logger = logger;
            _p2p = p2p;
        }
        
        public async Task Start()
        {
            ConsensusDisposable = SingleNodeTestObserver.SubscribeSingleNodeTestProcess();
            await Task.CompletedTask;
        }

        public async Task Update()
        {
            await Task.CompletedTask;
        }

        public async Task RecoverMining()
        {
            await Task.CompletedTask;
        }

        public bool IsAlive()
        {
            return true;
        }

        private async Task SingleNodeMining()
        {
            _logger.Trace("Single node mining start.");
            //var block = await _node.Mine();
            //await _p2p.BroadcastBlock(block);
            _logger.Trace("Single node mining end.");
        }
    }
}