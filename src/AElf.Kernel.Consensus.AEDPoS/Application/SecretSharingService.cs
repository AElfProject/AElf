using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Cryptography.SecretSharing;
using AElf.Kernel.Account.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class SecretSharingService : ISecretSharingService, ISingletonDependency
    {
        private readonly Dictionary<long, Dictionary<string, byte[]>> _sharingPieces =
            new Dictionary<long, Dictionary<string, byte[]>>();

        private readonly Dictionary<long, Dictionary<string, byte[]>> _revealedInValues =
            new Dictionary<long, Dictionary<string, byte[]>>();

        private readonly IRandomHashCacheService _randomHashCacheService;
        private readonly IAccountService _accountService;

        public SecretSharingService(IRandomHashCacheService randomHashCacheService, IAccountService accountService)
        {
            _randomHashCacheService = randomHashCacheService;
            _accountService = accountService;
        }

        public Task AddSharingInformationAsync(SecretSharingInformation secretSharingInformation)
        {
            // TODO: Generate in value and set it to _randomHashCacheService.
            var newInValue = Hash.FromMessage(secretSharingInformation);

            var pieces = new Dictionary<string, byte[]>();
            var revealedInValues = new Dictionary<string, byte[]>();

            var minersCount = secretSharingInformation.PreviousRound.RealTimeMinersInformation.Count;
            var minimumCount = minersCount.Mul(2).Div(3);
            var shares = SecretSharingHelper.EncodeSecret(newInValue.ToByteArray(), minimumCount, minersCount);
            foreach (var pair in secretSharingInformation.PreviousRound.RealTimeMinersInformation
                .OrderBy(m => m.Value.Order)
                .ToDictionary(m => m.Key, m => m.Value.Order))
            {
                // TODO: Sharing logic.
            }

            _sharingPieces[secretSharingInformation.CurrentRoundId] = pieces;

            // TODO:
            _revealedInValues[secretSharingInformation.CurrentRoundId] = revealedInValues;
            return Task.CompletedTask;
        }

        public Dictionary<string, byte[]> GetSharingPieces(long roundId)
        {
            _sharingPieces.TryGetValue(roundId, out var sharingPieces);
            _sharingPieces.Remove(roundId);
            return sharingPieces ?? new Dictionary<string, byte[]>();
        }

        public Dictionary<string, byte[]> GetRevealedInValues(long roundId)
        {
            _revealedInValues.TryGetValue(roundId, out var revealedInValues);
            _revealedInValues.Remove(roundId);
            return revealedInValues ?? new Dictionary<string, byte[]>();
        }
    }
}