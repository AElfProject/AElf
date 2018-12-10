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

        private readonly Func<Task> _nextTerm;
        private readonly Func<Task> _packageOutValue;
        private readonly Func<Task> _nextRound;

        private readonly IObservable<ConsensusBehavior> _nop = Observable
            .Timer(TimeSpan.FromSeconds(0))
            .Select(_ => ConsensusBehavior.NoOperationPerformed);

        private int Interval => ConsensusConfig.Instance.DPoSMiningInterval;

        public ConsensusObserver(params Func<Task>[] miningFunctions)
        {
            if (miningFunctions.Length < 3)
            {
                throw new ArgumentException("Incorrect functions count.", nameof(miningFunctions));
            }

            _logger = LogManager.GetLogger(nameof(ConsensusObserver));

            _nextTerm = miningFunctions[0];
            _packageOutValue = miningFunctions[1];
            _nextRound = miningFunctions[2];
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
                    _nextTerm();
                    break;
                case ConsensusBehavior.PackageOutValue:
                    _packageOutValue();
                    break;
                case ConsensusBehavior.NextRound:
                    _nextRound();
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

        public IDisposable SubscribeMiningProcess(Round roundInfo)
        {
            var welcome = roundInfo.RealTimeMinersInfo[NodeConfig.Instance.ECKeyPair.PublicKey.ToHex()];
            var extraBlockTimeSlot = roundInfo.GetEBPMiningTime(Interval).ToTimestamp();
            var myMiningTime = welcome.ExpectedMiningTime;
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

            var distanceToProduceExtraBlock = (extraBlockTimeSlot - now).Seconds;

            var produceExtraBlock = _nop;
            if (distanceToProduceExtraBlock < 0 && GlobalConfig.BlockProducerNumber != 1)
            {
                // No time, give up.
            }
            else if (welcome.IsExtraBlockProducer)
            {
                if (distanceToProduceExtraBlock >= 0)
                {
                    produceExtraBlock = Observable
                        .Timer(TimeSpan.FromSeconds(distanceToProduceExtraBlock - distanceToProduceNormalBlock))
                        .Select(_ => ConsensusBehavior.NextRound);
                    _logger?.Trace($"Will produce extra block after {distanceToProduceExtraBlock} seconds - " +
                                   $"{extraBlockTimeSlot.ToDateTime():HH:mm:ss.fff}.");
                }
            }
            else
            {
                var distanceToHelpProducingExtraBlock = distanceToProduceExtraBlock + Interval * welcome.Order / 1000;
                if (distanceToHelpProducingExtraBlock >= 0)
                {
                    produceExtraBlock = Observable
                        .Timer(TimeSpan.FromSeconds(distanceToHelpProducingExtraBlock - distanceToProduceNormalBlock))
                        .Select(_ => ConsensusBehavior.NextRound);
                    _logger?.Trace($"Will help to produce extra block after {distanceToHelpProducingExtraBlock} seconds - " +
                                   $"{extraBlockTimeSlot.ToDateTime().AddMilliseconds(Interval * welcome.Order):HH:mm:ss.fff}");
                }
            }

            return Observable.Return(ConsensusBehavior.NoOperationPerformed)
                .Concat(produceNormalBlock)
                .Concat(produceExtraBlock)
                .SubscribeOn(NewThreadScheduler.Default)
                .Subscribe(this);
        }
    }
}