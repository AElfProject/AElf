using System;
using System.Threading.Tasks;
using AElf.Kernel.Consensus;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    public class StandaloneNodeConsensusPlaceHolder : IConsensus
    {
        public IDisposable ConsensusDisposable { get; set; }
        private readonly ILogger _logger;
        private SingleNodeTestObserver SingleNodeTestObserver => new SingleNodeTestObserver(_logger, SingleNodeMining);
        
        public StandaloneNodeConsensusPlaceHolder()
        {
            _logger = LogManager.GetLogger(nameof(StandaloneNodeConsensusPlaceHolder));
        }
        
        public async Task Start()
        {
            ConsensusDisposable = SingleNodeTestObserver.SubscribeSingleNodeTestProcess();
            await Task.CompletedTask;
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Hang()
        {
            throw new NotImplementedException();
        }

        public void Recover()
        {
            throw new NotImplementedException();
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