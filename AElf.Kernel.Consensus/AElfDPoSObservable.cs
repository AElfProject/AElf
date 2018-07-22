using System;
using System.Threading.Tasks;
using Google.Protobuf;
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
        private readonly Func<Task> _miningWithPublishingInValue;
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
            _miningWithPublishingInValue = minings[2];
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
                case ConsensusBehavior.InitializeAElfDPoS:
                    _miningWithInitializingAElfDPoSInformation();
                    break;
                case ConsensusBehavior.PublishOutValueAndSignature:
                    _miningWithPublishingOutValueAndSignature();
                    break;
                case ConsensusBehavior.PublishInValue:
                    _miningWithPublishingInValue();
                    break;
                case ConsensusBehavior.UpdateAElfDPoS:
                    _miningWithUpdatingAElfDPoSInformation();
                    break;
            }
        }
    }
}