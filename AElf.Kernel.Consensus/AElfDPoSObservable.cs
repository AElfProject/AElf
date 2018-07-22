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
        private readonly Func<Task> _broadcastInitializeAElfDPoSTx;
        private readonly Func<Task> _broadcastPublishOutValueAndSignatureTx;
        private readonly Func<Task> _broadcastPublishInValueTx;
        // ReSharper disable once InconsistentNaming
        private readonly Func<Task> _broadcastUpdateAElfDPoSTx;

        public AElfDPoSObservable(ILogger logger, params Func<Task>[] broadcasts)
        {
            if (broadcasts.Length < 4)
            {
                throw new ArgumentException("broadcasts count incorrect.", nameof(broadcasts));
            }

            _logger = logger;

            _broadcastInitializeAElfDPoSTx = broadcasts[0]; 
            _broadcastPublishOutValueAndSignatureTx = broadcasts[1];
            _broadcastPublishInValueTx = broadcasts[2];
            _broadcastUpdateAElfDPoSTx = broadcasts[3];
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
            var behavior = nameof(value);
            switch (behavior)
            {
                case nameof(ConsensusBehavior.InitializeAElfDPoS):
                    _broadcastInitializeAElfDPoSTx.Invoke();
                    break;
                case nameof(ConsensusBehavior.PublishOutValueAndSignature):
                    _broadcastPublishOutValueAndSignatureTx();
                    break;
                case nameof(ConsensusBehavior.PublishInValue):
                    _broadcastPublishInValueTx();
                    break;
                case nameof(ConsensusBehavior.UpdateAElfDPoS):
                    _broadcastUpdateAElfDPoSTx();
                    break;
            }
        }
    }
}