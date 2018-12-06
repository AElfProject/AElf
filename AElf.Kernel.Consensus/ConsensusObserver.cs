using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using NLog;
using AElf.Common;
using AElf.Configuration.Config.Consensus;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable InconsistentNaming
    public class ConsensusObserver : IObserver<ConsensusBehavior>
    {
        private readonly ILogger _logger;

        private readonly Func<Task> _miningWithInitializingAElfDPoSInformation;
        private readonly Func<Task> _miningWithPublishingOutValueAndSignature;
        private readonly Func<Task> _publishInValue;

        private readonly Func<Task> _miningWithUpdatingAElfDPoSInformation;

        public ConsensusObserver(params Func<Task>[] miningFunctions)
        {
            if (miningFunctions.Length < 4)
            {
                throw new ArgumentException("Incorrect functions count.", nameof(miningFunctions));
            }

            _logger = LogManager.GetLogger(nameof(ConsensusObserver));

            _miningWithInitializingAElfDPoSInformation = miningFunctions[0];
            _miningWithPublishingOutValueAndSignature = miningFunctions[1];
            _publishInValue = miningFunctions[2];
            _miningWithUpdatingAElfDPoSInformation = miningFunctions[3];
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
                    _logger?.Trace("DPoS NOP.");
                    break;
                case ConsensusBehavior.InitializeAElfDPoS:
                    _miningWithInitializingAElfDPoSInformation();
                    break;
                case ConsensusBehavior.PublishOutValueAndSignature:
                    _miningWithPublishingOutValueAndSignature();
                    break;
                case ConsensusBehavior.PublishInValue:
                    _publishInValue();
                    break;
                case ConsensusBehavior.UpdateAElfDPoS:
                    _miningWithUpdatingAElfDPoSInformation();
                    break;
            }
        }

        public IDisposable Initialization()
        {
            var timeWaitingOtherNodes = TimeSpan.FromMilliseconds(ConsensusConfig.Instance.DPoSMiningInterval * 2.5);
            var delayInitialize = Observable
                .Timer(timeWaitingOtherNodes)
                .Select(_ => ConsensusBehavior.InitializeAElfDPoS);
            _logger?.Trace(
                $"Will initialize dpos information after {timeWaitingOtherNodes.TotalSeconds} seconds - " +
                $"{DateTime.UtcNow.AddMilliseconds(timeWaitingOtherNodes.TotalMilliseconds):HH:mm:ss.fff}.");
            return Observable.Return(ConsensusBehavior.NoOperationPerformed).Concat(delayInitialize).Subscribe(this);
        }

        public IDisposable RecoverMining()
        {
            var timeSureToRecover = TimeSpan.FromMilliseconds(
                ConsensusConfig.Instance.DPoSMiningInterval * GlobalConfig.BlockProducerNumber);
            var recoverMining = Observable
                .Timer(timeSureToRecover)
                .Select(_ => ConsensusBehavior.UpdateAElfDPoS);

            _logger?.Trace(
                $"Will produce extra block after {timeSureToRecover.Seconds} seconds due to recover mining process - " +
                $"{DateTime.UtcNow.AddMilliseconds(timeSureToRecover.TotalMilliseconds):HH:mm:ss.fff}.");

            return Observable.Return(ConsensusBehavior.NoOperationPerformed)
                .Concat(recoverMining)
                .Subscribe(this);
        }

        public IDisposable SubscribeAElfDPoSMiningProcess(BlockProducer infoOfMe, Timestamp extraBlockTimeSlot)
        {
            var nopObservable = Observable
                .Timer(TimeSpan.FromSeconds(0))
                .Select(_ => ConsensusBehavior.NoOperationPerformed);

            var timeSlot = infoOfMe.TimeSlot;
            var now = DateTime.UtcNow.ToTimestamp();
            var distanceToProduceNormalBlock = (timeSlot - now).Seconds;

            IObservable<ConsensusBehavior> produceNormalBlock;
            if (distanceToProduceNormalBlock >= 0)
            {
                produceNormalBlock = Observable
                    .Timer(TimeSpan.FromSeconds(distanceToProduceNormalBlock))
                    .Select(_ => ConsensusBehavior.PublishOutValueAndSignature);

                if (distanceToProduceNormalBlock >= 0)
                {
                    _logger?.Trace($"Will produce normal block after {distanceToProduceNormalBlock} seconds - {timeSlot.ToDateTime():HH:mm:ss.fff}.");
                }
            }
            else
            {
                distanceToProduceNormalBlock = 0;
                produceNormalBlock = nopObservable;
            }

            var distanceToPublishInValue = (extraBlockTimeSlot - now).Seconds;

            IObservable<ConsensusBehavior> publishInValue;
            if (distanceToPublishInValue >= 0)
            {
                var after = distanceToPublishInValue - distanceToProduceNormalBlock;
                publishInValue = Observable
                    .Timer(TimeSpan.FromSeconds(after))
                    .Select(_ => ConsensusBehavior.PublishInValue);

                if (distanceToPublishInValue >= 0)
                {
                    _logger?.Trace($"Will publish in value after {distanceToPublishInValue} seconds - {extraBlockTimeSlot.ToDateTime():HH:mm:ss.fff}.");
                }
            }
            else
            {
                publishInValue = nopObservable;
            }

            IObservable<ConsensusBehavior> produceExtraBlock;
            if (distanceToPublishInValue < 0 && GlobalConfig.BlockProducerNumber != 1)
            {
                produceExtraBlock = nopObservable;
                if (GlobalConfig.BlockProducerNumber != 1)
                {
                    produceExtraBlock = nopObservable;
                }
            }
            else if (infoOfMe.IsEBP)
            {
                var after = distanceToPublishInValue + ConsensusConfig.Instance.DPoSMiningInterval / 1000;
                produceExtraBlock = Observable
                    .Timer(TimeSpan.FromMilliseconds(ConsensusConfig.Instance.DPoSMiningInterval))
                    .Select(_ => ConsensusBehavior.UpdateAElfDPoS);

                if (after >= 0)
                {
                    _logger?.Trace($"Will produce extra block after {after} seconds - {extraBlockTimeSlot.ToDateTime().AddMilliseconds(4000):HH:mm:ss.fff}.");
                }
            }
            else
            {
                var after = distanceToPublishInValue + ConsensusConfig.Instance.DPoSMiningInterval / 1000 +
                            ConsensusConfig.Instance.DPoSMiningInterval * infoOfMe.Order / 1000 +
                            ConsensusConfig.Instance.DPoSMiningInterval / 750;
                produceExtraBlock = Observable
                    .Timer(TimeSpan.FromMilliseconds(ConsensusConfig.Instance.DPoSMiningInterval +
                                                     ConsensusConfig.Instance.DPoSMiningInterval * infoOfMe.Order +
                                                     ConsensusConfig.Instance.DPoSMiningInterval / 2))
                    .Select(_ => ConsensusBehavior.UpdateAElfDPoS);

                if (after >= 0)
                {
                    _logger?.Trace($"Will help to produce extra block after {after} seconds.");
                }
            }

            return Observable.Return(ConsensusBehavior.NoOperationPerformed)
                .Concat(produceNormalBlock)
                .Concat(publishInValue)
                .Concat(produceExtraBlock)
                .SubscribeOn(NewThreadScheduler.Default)
                .Subscribe(this);
        }
    }
}