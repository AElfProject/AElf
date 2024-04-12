using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.Contracts.Economic.TestBase;

public partial class EconomicContractsTestBase
{
    #region Other Contracts Action and View

    protected async Task NextTerm(ECKeyPair keyPair)
    {
        var miner = GetConsensusContractTester(keyPair);
        var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
        var victories = await ElectionContractStub.GetVictories.CallAsync(new Empty());
        var miners = new MinerList
        {
            Pubkeys =
            {
                victories.Value
            }
        };
        var randomNumber = await GenerateRandomProofAsync(keyPair);
        var firstRoundOfNextTerm =
            miners.GenerateFirstRoundOfNewTerm(EconomicContractsTestConstants.MiningInterval,
                randomNumber, BlockTimeProvider.GetBlockTime(), round.RoundNumber, round.TermNumber);
        var executionResult = (await miner.NextTerm.SendAsync(firstRoundOfNextTerm)).TransactionResult;
        executionResult.Error.ShouldBeNullOrEmpty();
        executionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    protected async Task NextRound(ECKeyPair keyPair)
    {
        var miner = GetConsensusContractTester(keyPair);
        var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
        var randomNumber = await GenerateRandomProofAsync(keyPair);
        round.GenerateNextRoundInformation(
            StartTimestamp.ToDateTime().AddMilliseconds(round.TotalMilliseconds()).ToTimestamp(), StartTimestamp,
            ByteString.CopyFrom(randomNumber), out var nextRound);
        await miner.NextRound.SendAsync(nextRound);
    }

    protected async Task NormalBlock(ECKeyPair keyPair)
    {
        var miner = GetConsensusContractTester(keyPair);
        var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
        var minerInRound = round.RealTimeMinersInformation[keyPair.PublicKey.ToHex()];

        await miner.UpdateValue.SendAsync(new UpdateValueInput
        {
            OutValue = minerInRound.OutValue,
            Signature = minerInRound.Signature,
            PreviousInValue = minerInRound.PreviousInValue ?? Hash.Empty,
            RoundId = round.RoundId,
            ProducedBlocks = minerInRound.ProducedBlocks + 1,
            ActualMiningTime = minerInRound.ExpectedMiningTime,
            SupposedOrderOfNextRound = 1,
            RandomNumber = ByteString.CopyFrom(await GenerateRandomProofAsync(keyPair))
        });
    }

    protected async Task ProduceBlocks(ECKeyPair keyPair, int roundsCount, bool changeTerm = false)
    {
        for (var i = 0; i < roundsCount; i++)
        {
            await NormalBlock(keyPair);
            if (i != roundsCount - 1) continue;
            if (changeTerm)
                await NextTerm(keyPair);
            else
                await NextRound(keyPair);
        }
    }

    protected async Task<long> GetNativeTokenBalance(byte[] publicKey)
    {
        var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = EconomicContractsTestConstants.NativeTokenSymbol,
            Owner = Address.FromPublicKey(publicKey)
        })).Balance;

        return balance;
    }

    protected async Task<long> GetVoteTokenBalance(byte[] publicKey)
    {
        var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = EconomicContractsTestConstants.VoteSymbol,
            Owner = Address.FromPublicKey(publicKey)
        })).Balance;

        return balance;
    }

    protected async Task<byte[]> GenerateRandomProofAsync(ECKeyPair keyPair)
    {
        var consensusContractStub = GetConsensusContractTester(keyPair);
        var blockHeight = (await BlockchainService.GetChainAsync()).BestChainHeight;
        var previousRandomHash =
            blockHeight <= 1
                ? Hash.Empty
                : await consensusContractStub.GetRandomHash.CallAsync(new Int64Value
                    { Value = blockHeight });
        return CryptoHelper.ECVrfProve(keyPair, previousRandomHash.ToByteArray());
    }

    #endregion
}