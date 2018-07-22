using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable once InconsistentNaming
    public class AElfDPoSObservable : IObserver<ConsensusBehavior>
    {
        private readonly ILogger _logger;
        
        // ReSharper disable once InconsistentNaming
        private readonly Func<Task> _miningWithInitializingAElfDPoSInformation;
        private readonly Func<Task> _miningWithPublishingOutValueAndSignature;
        private readonly Func<Task> _publishInValue;
        // ReSharper disable once InconsistentNaming
        private readonly Func<Task> _miningWithUpdatingAElfDPoSInformation;

        public AElfDPoSObservable(ILogger logger, params Func<Task>[] minings)
        {
            if (minings.Length < 4)
            {
                throw new ArgumentException("broadcasts count incorrect.", nameof(minings));
            }

            _logger = logger;

            _miningWithInitializingAElfDPoSInformation = minings[0]; 
            _miningWithPublishingOutValueAndSignature = minings[1];
            _publishInValue = minings[2];
            _miningWithUpdatingAElfDPoSInformation = minings[3];
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
                    _logger?.Trace("Start a new round.");
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

        public static IDisposable Initialization(IObserver<ConsensusBehavior> observer)
        {
            return new List<ConsensusBehavior>
                {
                    ConsensusBehavior.InitializeAElfDPoS
                }
                .ToObservable()
                .Subscribe(observer);
        }

        public static IDisposable NormalMiningProcess(BPInfo infoOfMe, Timestamp extraBlockTimeslot, IObserver<ConsensusBehavior> observer)
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

                Console.WriteLine("concat: normal block");
            }
            else
            {
                distanceToProduceNormalBlock = 0;
                produceNormalBlock = doNothingObservable;
            }

            IObservable<ConsensusBehavior> publishInValue;
            var distanceToPublishInValue = (extraBlockTimeslot - now).Seconds;
            if (distanceToPublishInValue >= 0)
            {
                publishInValue = Observable
                        .Timer(TimeSpan.FromSeconds(distanceToPublishInValue - distanceToProduceNormalBlock))
                        .Select(_ => ConsensusBehavior.PublishInValue);

                Console.WriteLine("concat: publish in value");
            }
            else
            {
                distanceToPublishInValue = 0;
                publishInValue = doNothingObservable;
            }

            IObservable<ConsensusBehavior> produceExtraBlock;
            if (infoOfMe.IsEBP)
            {
                produceExtraBlock = Observable
                    .Timer(TimeSpan.FromSeconds(distanceToPublishInValue - distanceToProduceNormalBlock + Globals.AElfMiningTime))
                    .Select(_ => ConsensusBehavior.UpdateAElfDPoS);

                Console.WriteLine("concat: update dpos information");
            }
            else
            {
                produceExtraBlock = Observable
                    .Timer(TimeSpan.FromSeconds(distanceToPublishInValue - distanceToProduceNormalBlock +
                                                Globals.AElfMiningTime + Globals.AElfMiningTime * infoOfMe.Order))
                    .Select(_ => ConsensusBehavior.UpdateAElfDPoS);

                Console.WriteLine("concat: help to update dpos information");
            }

            return Observable.Return(ConsensusBehavior.DoNothing)
                .Concat(produceNormalBlock)
                .Concat(publishInValue)
                .Concat(produceExtraBlock)
                .Subscribe(observer);
        }
    }
}