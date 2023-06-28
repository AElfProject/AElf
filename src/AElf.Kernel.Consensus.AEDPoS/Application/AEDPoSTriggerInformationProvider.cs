using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.AEDPoS.Application;

internal class AEDPoSTriggerInformationProvider : ITriggerInformationProvider
{
    private readonly IAccountService _accountService;
    private readonly IInValueCache _inValueCache;
    private readonly ISecretSharingService _secretSharingService;
    private readonly IRandomNumberProvider _randomNumberProvider;

    public AEDPoSTriggerInformationProvider(IAccountService accountService,
        ISecretSharingService secretSharingService, IInValueCache inValueCache, IRandomNumberProvider randomNumberProvider)
    {
        _accountService = accountService;
        _secretSharingService = secretSharingService;
        _inValueCache = inValueCache;
        _randomNumberProvider = randomNumberProvider;

        Logger = NullLogger<AEDPoSTriggerInformationProvider>.Instance;
    }

    private ByteString Pubkey => ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync));

    public ILogger<AEDPoSTriggerInformationProvider> Logger { get; set; }

    public BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes)
    {
        return new BytesValue { Value = Pubkey };
    }

    public BytesValue GetTriggerInformationForBlockHeaderExtraData(BytesValue consensusCommandBytes)
    {
        if (consensusCommandBytes == null)
            return new AElfConsensusTriggerInformation
            {
                Pubkey = Pubkey,
                Behaviour = AElfConsensusBehaviour.UpdateValue
            }.ToBytesValue();

        var command = consensusCommandBytes.ToConsensusCommand();
        var hint = command.Hint.ToAElfConsensusHint();

        if (hint.Behaviour == AElfConsensusBehaviour.UpdateValue)
        {
            var newInValue = _inValueCache.GetInValue(hint.RoundId);
            var previousInValue = _inValueCache.GetInValue(hint.PreviousRoundId);
            Logger.LogDebug($"New in value {newInValue} for round of id {hint.RoundId}");
            Logger.LogDebug($"Previous in value {previousInValue} for round of id {hint.PreviousRoundId}");
            var trigger = new AElfConsensusTriggerInformation
            {
                Pubkey = Pubkey,
                InValue = newInValue,
                PreviousInValue = previousInValue,
                Behaviour = hint.Behaviour
            };

            return trigger.ToBytesValue();
        }

        return new AElfConsensusTriggerInformation
        {
            Pubkey = Pubkey,
            Behaviour = hint.Behaviour
        }.ToBytesValue();
    }

    public BytesValue GetTriggerInformationForConsensusTransactions(IChainContext chainContext, BytesValue consensusCommandBytes)
    {
        var randomProof = AsyncHelper.RunSync(async ()=> await _randomNumberProvider.GenerateRandomProofAsync(chainContext));
        
        if (consensusCommandBytes == null)
            return new AElfConsensusTriggerInformation
            {
                Pubkey = Pubkey,
                Behaviour = AElfConsensusBehaviour.UpdateValue,
                RandomNumber = ByteString.CopyFrom(randomProof)
            }.ToBytesValue();

        var command = consensusCommandBytes.ToConsensusCommand();
        var hint = command.Hint.ToAElfConsensusHint();

        if (hint.Behaviour == AElfConsensusBehaviour.UpdateValue)
        {
            var inValue = _inValueCache.GetInValue(hint.RoundId);
            var trigger = new AElfConsensusTriggerInformation
            {
                Pubkey = Pubkey,
                InValue = inValue,
                PreviousInValue = _inValueCache.GetInValue(hint.PreviousRoundId),
                Behaviour = hint.Behaviour,
                RandomNumber = ByteString.CopyFrom(randomProof)
            };

            var secretPieces = _secretSharingService.GetEncryptedPieces(hint.RoundId);
            foreach (var secretPiece in secretPieces)
                trigger.EncryptedPieces.Add(secretPiece.Key, ByteString.CopyFrom(secretPiece.Value));

            var decryptedPieces = _secretSharingService.GetDecryptedPieces(hint.RoundId);
            foreach (var decryptedPiece in decryptedPieces)
                trigger.DecryptedPieces.Add(decryptedPiece.Key, ByteString.CopyFrom(decryptedPiece.Value));

            var revealedInValues = _secretSharingService.GetRevealedInValues(hint.RoundId);
            foreach (var revealedInValue in revealedInValues)
                trigger.RevealedInValues.Add(revealedInValue.Key, revealedInValue.Value);

            return trigger.ToBytesValue();
        }

        return new AElfConsensusTriggerInformation
        {
            Pubkey = Pubkey,
            Behaviour = hint.Behaviour,
            RandomNumber = ByteString.CopyFrom(randomProof)
        }.ToBytesValue();
    }
}