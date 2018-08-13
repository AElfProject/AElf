using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Node.Protocol;
using AElf.SmartContract;
using NLog;

namespace AElf.Kernel.Node
{
    public class MinerHelper
    {
        private readonly ILogger _logger;
        private readonly IWorldStateDictator _worldStateDictator;
        private int _flag;

        private MainChainNode Node { get; }

        private ECKeyPair NodeKeyPair
        {
            get => Node.NodeKeyPair;
        }

        private readonly IMiner _miner;
        private readonly Consensus _consensus;
        private readonly IBlockSynchronizer _synchronizer;

        public MinerHelper(ILogger logger, MainChainNode node, IWorldStateDictator worldStateDictator,
            IMiner miner, Consensus consensus, IBlockSynchronizer synchronizer)
        {
            _logger = logger;
            Node = node;
            _worldStateDictator = worldStateDictator;
            _miner = miner;
            _consensus = consensus;
            _synchronizer = synchronizer;
        }

        public async Task<IBlock> Mine()
        {
            var res = Interlocked.CompareExchange(ref _flag, 1, 0);
            if (res == 1)
                return null;
            try
            {
                _logger?.Trace($"Mine - Entered mining {res}");

                _worldStateDictator.BlockProducerAccountAddress = NodeKeyPair.GetAddress();

                var task = Task.Run(async () => await _miner.Mine());

                if (!task.Wait(TimeSpan.FromMilliseconds(Globals.AElfDPoSMiningInterval * 0.9)))
                {
                    _logger?.Error("Mining timeout.");
                    return null;
                }

                var b = Interlocked.CompareExchange(ref _flag, 0, 1);

                _synchronizer.IncrementChainHeight();

                _logger?.Trace($"Mine - Leaving mining {b}");

                Task.WaitAll();

                //Update DPoS observables.
                //Sometimes failed to update this observables list (which is weird), just ignore this.
                //Which means this node will do nothing in this round.
                try
                {
                    await Node.CheckUpdatingConsensusProcess();
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Somehow failed to update DPoS observables. Will recover soon.");
                    //In case just config one node to produce blocks.
                    _consensus.AElfDPoSObserver.RecoverMining();
                }

                return task.Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Interlocked.CompareExchange(ref _flag, 0, 1);
                return null;
            }
        }
    }
}