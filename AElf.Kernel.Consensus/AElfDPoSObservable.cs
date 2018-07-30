using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable once InconsistentNaming
    [LoggerName(nameof(AElfDPoSObservable))]
    public class AElfDPoSObservable : IObserver<ConsensusBehavior>
    {
        private readonly ILogger _logger;
        
        // ReSharper disable once InconsistentNaming
        private readonly Func<Task> _miningWithInitializingAElfDPoSInformation;
        private readonly Func<Task> _miningWithPublishingOutValueAndSignature;
        private readonly Func<Task> _publishInValue;
        // ReSharper disable once InconsistentNaming
        private readonly Func<Task> _miningWithUpdatingAElfDPoSInformation;

        public AElfDPoSObservable(ILogger logger, params Func<Task>[] miningFunctions)
        {
            if (miningFunctions.Length < 4)
            {
                throw new ArgumentException("Incorrect functions count.", nameof(miningFunctions));
            }

            _logger = logger;

            _miningWithInitializingAElfDPoSInformation = miningFunctions[0]; 
            _miningWithPublishingOutValueAndSignature = miningFunctions[1];
            _publishInValue = miningFunctions[2];
            _miningWithUpdatingAElfDPoSInformation = miningFunctions[3];
        }

        public void OnCompleted()
        {
            _logger?.Trace($"{nameof(AElfDPoSObservable)} completed.");
        }

        public void OnError(Exception error)
        {
            _logger?.Error(error, $"{nameof(AElfDPoSObservable)} error.");
        }

        public void OnNext(ConsensusBehavior value)
        {
            switch (value)
            {
                case ConsensusBehavior.DoNothing:
                    _logger?.Trace("Start a new round though this behavior doing nothing.");
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

        public void Initialization()
        {
            Observable.Return(ConsensusBehavior.InitializeAElfDPoS).Subscribe(this);
        }
        
        public IDisposable SubscribeMiningProcess(BPInfo infoOfMe, Timestamp extraBlockTimeslot)
        {
            var doNothingObservable = Observable
                .Timer(TimeSpan.FromSeconds(0))
                .Select(_ => ConsensusBehavior.DoNothing);

            var timeslot = infoOfMe.TimeSlot;
            var now = DateTime.UtcNow.ToTimestamp();
            var distanceToProduceNormalBlock = (timeslot - now).Seconds;
            
            IObservable<ConsensusBehavior> produceNormalBlock;
            if (distanceToProduceNormalBlock >= 0)
            {
                produceNormalBlock = Observable
                        .Timer(TimeSpan.FromSeconds(distanceToProduceNormalBlock))
                        .Select(_ => ConsensusBehavior.PublishOutValueAndSignature);

                _logger?.Trace($"Will produce normal block after {distanceToProduceNormalBlock} seconds");
            }
            else
            {
                distanceToProduceNormalBlock = 0;
                produceNormalBlock = doNothingObservable;
            }

            var distanceToPublishInValue = (extraBlockTimeslot - now).Seconds;
            
            IObservable<ConsensusBehavior> publishInValue;
            if (distanceToPublishInValue >= 0)
            {
                var after = distanceToPublishInValue - distanceToProduceNormalBlock;
                publishInValue = Observable
                        .Timer(TimeSpan.FromSeconds(after))
                        .Select(_ => ConsensusBehavior.PublishInValue);

                _logger?.Trace($"Will publish in value after {distanceToPublishInValue} seconds");
            }
            else
            {
                publishInValue = doNothingObservable;
            }

            IObservable<ConsensusBehavior> produceExtraBlock;
            if (distanceToPublishInValue <= 0)
            {
                produceExtraBlock = doNothingObservable;
            }
            else if (infoOfMe.IsEBP)
            {
                var after = distanceToPublishInValue + Globals.AElfMiningTime / 1000;
                produceExtraBlock = Observable
                    .Timer(TimeSpan.FromMilliseconds(Globals.AElfMiningTime))
                    .Select(_ => ConsensusBehavior.UpdateAElfDPoS);

                _logger?.Trace($"Will produce extra block after {after} seconds"); 
            }
            else
            {
                var after = distanceToPublishInValue + Globals.AElfMiningTime / 1000 +
                            Globals.AElfMiningTime * infoOfMe.Order / 1000;
                produceExtraBlock = Observable
                    .Timer(TimeSpan.FromMilliseconds(Globals.AElfMiningTime + Globals.AElfMiningTime * infoOfMe.Order))
                    .Select(_ => ConsensusBehavior.UpdateAElfDPoS);

                _logger?.Trace($"Will help to produce extra block after {after} seconds");
            }

            return Observable.Return(ConsensusBehavior.DoNothing)
                .Concat(produceNormalBlock)
                .Concat(publishInValue)
                .Concat(produceExtraBlock)
                .Subscribe(this);
        }
    }
}