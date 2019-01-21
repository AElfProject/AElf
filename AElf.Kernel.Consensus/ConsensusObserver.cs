using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using NLog;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Consensus;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable InconsistentNaming
    public class ConsensusObserver : IObserver<ConsensusBehavior>
    {
        private readonly ILogger _logger;

        private readonly Func<Task> _initialTerm;
        private readonly Func<Task> _packageOutValue;
        private readonly Func<Task> _broadcastInValue;
        private readonly Func<Task> _nextRound;
        private readonly Func<Task> _nextTerm;

        private readonly IObservable<ConsensusBehavior> _nop = Observable
            .Timer(TimeSpan.FromSeconds(0))
            .Select(_ => ConsensusBehavior.NoOperationPerformed);

        private int Interval => ConsensusConfig.Instance.DPoSMiningInterval;

        public ConsensusObserver(params Func<Task>[] miningFunctions)
        {
            if (miningFunctions.Length != 5)
            {
                throw new ArgumentException("Incorrect functions count.", nameof(miningFunctions));
            }

            _logger = LogManager.GetLogger(nameof(ConsensusObserver));

            _initialTerm = miningFunctions[0];
            _packageOutValue = miningFunctions[1];
            _broadcastInValue = miningFunctions[2];
            _nextRound = miningFunctions[3];
            _nextTerm = miningFunctions[4];
        }

        public void OnCompleted()
        {
            _logger?.Trace($"{nameof(ConsensusObserver)} completed.");
        }

        public void OnError(Exception error)
        {
            _logger?.Error(error, $"{nameof(ConsensusObserver)} error.");
        }

        public void OnNext(ConsensusBehavior value)
        {
            switch (value)
            {
                case ConsensusBehavior.NoOperationPerformed:
                    break;
                case ConsensusBehavior.InitialTerm:
                    _initialTerm();
                    break;
                case ConsensusBehavior.PackageOutValue:
                    _packageOutValue();
                    break;
                case ConsensusBehavior.BroadcastInValue:
                    _broadcastInValue();
                    break;
                case ConsensusBehavior.NextRound:
                    _nextRound();
                    break;
                case ConsensusBehavior.NextTerm:
                    _nextTerm();
                    break;
            }
        }

        public IDisposable Initialization()
        {
            var timeWaitingOtherNodes = TimeSpan.FromMilliseconds(Interval * 2.5);
            var delayInitialize = Observable
                .Timer(timeWaitingOtherNodes)
                .Select(_ => ConsensusBehavior.InitialTerm);
            _logger?.Trace(
                $"Will initialize next term information after {timeWaitingOtherNodes.TotalSeconds} seconds - " +
                $"{DateTime.UtcNow.AddMilliseconds(timeWaitingOtherNodes.TotalMilliseconds):HH:mm:ss.fff}.");
            return Observable.Return(ConsensusBehavior.NoOperationPerformed).Concat(delayInitialize).Subscribe(this);
        }

        public IDisposable RecoverMining()
        {
            var timeSureToRecover = TimeSpan.FromMilliseconds(
                Interval * GlobalConfig.BlockProducerNumber);
            var recoverMining = Observable
                .Timer(timeSureToRecover)
                .Select(_ => ConsensusBehavior.NextRound);

            _logger?.Trace(
                $"Will produce extra block after {timeSureToRecover.Seconds} seconds due to recover mining process - " +
                $"{DateTime.UtcNow.AddMilliseconds(timeSureToRecover.TotalMilliseconds):HH:mm:ss.fff}.");

            return Observable.Return(ConsensusBehavior.NoOperationPerformed)
                .Concat(recoverMining)
                .Subscribe(this);
        }

        public IDisposable NextTerm()
        {
            return Observable.Return(ConsensusBehavior.NextTerm).Subscribe(this);
        }

        public IDisposable SubscribeMiningProcess(Round roundInformation)
        {
            if (roundInformation?.RealTimeMinersInfo == null)
            {
                _logger?.Trace("Round information is null.");
                return null;
            }
            
            var publicKey = NodeConfig.Instance.ECKeyPair.PublicKey.ToHex();
            
            if (!roundInformation.RealTimeMinersInfo.ContainsKey(publicKey))
            {
                _logger?.Trace("This node isn't current miner.");
                // This node isn't current miner.
                return null;
            }
            
            _logger?.Trace("Using round number: " + roundInformation.RoundNumber);
            
            _logger?.Trace("Start - Subscribe consensus events.");
            
            var profile = roundInformation.RealTimeMinersInfo[publicKey];
            var extraBlockTimeSlot = roundInformation.GetEBPMiningTime(Interval).ToTimestamp();
            var myMiningTime = profile.ExpectedMiningTime;
            
            var now = DateTime.UtcNow.ToTimestamp();
            var distanceToProduceNormalBlock = (myMiningTime - now).Seconds;

            var produceNormalBlock = _nop;
            if (distanceToProduceNormalBlock >= 0)
            {
                produceNormalBlock = Observable
                    .Timer(TimeSpan.FromSeconds(distanceToProduceNormalBlock))
                    .Select(_ => ConsensusBehavior.PackageOutValue);

                _logger?.Trace($"Will produce normal block after {distanceToProduceNormalBlock} seconds - " +
                               $"{myMiningTime.ToDateTime():HH:mm:ss.fff}.");
            }
            else
            {
                // Single node can recover mining if something wrong happened to the process of node.
                if (GlobalConfig.BlockProducerNumber == 1)
                {
                    return RecoverMining();
                }
            }

            var distanceToProduceExtraBlock = (extraBlockTimeSlot - now).Seconds;

            var produceExtraBlock = _nop;
            var produceAnotherExtraBlock = _nop;

            if (distanceToProduceExtraBlock < 0 && GlobalConfig.BlockProducerNumber != 1)
            {
                // No time, give up.
            }
            else if (profile.IsExtraBlockProducer)
            {
                if (distanceToProduceExtraBlock >= 0)
                {
                    produceExtraBlock = Observable
                        .Timer(TimeSpan.FromSeconds(distanceToProduceExtraBlock - distanceToProduceNormalBlock))
                        .Select(_ => ConsensusBehavior.NextRound);
                    _logger?.Trace($"Will produce extra block after {distanceToProduceExtraBlock} seconds - " +
                                   $"{extraBlockTimeSlot.ToDateTime():HH:mm:ss.fff}.");
                    produceAnotherExtraBlock = Observable
                        .Timer(TimeSpan.FromSeconds(GlobalConfig.BlockProducerNumber * Interval / 1000))
                        .Select(_ => ConsensusBehavior.NextRound);
                    _logger?.Trace(
                        $"Will produce another extra block after {distanceToProduceExtraBlock + GlobalConfig.BlockProducerNumber * Interval / 1000} seconds.");
                }
            }
            else
            {
                var distanceToHelpProducingExtraBlock = distanceToProduceExtraBlock + Interval * profile.Order / 1000;
                if (distanceToHelpProducingExtraBlock >= 0)
                {
                    produceExtraBlock = Observable
                        .Timer(TimeSpan.FromSeconds(distanceToHelpProducingExtraBlock - distanceToProduceNormalBlock))
                        .Select(_ => ConsensusBehavior.NextRound);
                    _logger?.Trace(
                        $"Will help to produce extra block after {distanceToHelpProducingExtraBlock} seconds - " +
                        $"{extraBlockTimeSlot.ToDateTime().AddMilliseconds(Interval * profile.Order):HH:mm:ss.fff}");
                    produceAnotherExtraBlock = Observable
                        .Timer(TimeSpan.FromSeconds(GlobalConfig.BlockProducerNumber * Interval / 1000))
                        .Select(_ => ConsensusBehavior.NextRound);
                    _logger?.Trace(
                        $"Will produce another extra block after {distanceToHelpProducingExtraBlock + GlobalConfig.BlockProducerNumber * Interval / 1000} seconds.");
                }
            }
            
            _logger?.Trace("End - Subscribe consensus events.");

            return Observable.Return(ConsensusBehavior.NoOperationPerformed)
                .Concat(produceNormalBlock)
                .Concat(produceExtraBlock)
                .Concat(produceAnotherExtraBlock)
                .SubscribeOn(NewThreadScheduler.Default)
                .Subscribe(this);
        }
    }
}