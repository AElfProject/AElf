using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.Contracts.Economic.TestBase
{
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
            var firstRoundOfNextTerm =
                miners.GenerateFirstRoundOfNewTerm(EconomicContractsTestConstants.MiningInterval,
                    BlockTimeProvider.GetBlockTime(), round.RoundNumber, round.TermNumber);
            var executionResult = (await miner.NextTerm.SendAsync(firstRoundOfNextTerm)).TransactionResult;
            executionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        protected async Task NextRound(ECKeyPair keyPair)
        {
            var miner = GetConsensusContractTester(keyPair);
            var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
            round.GenerateNextRoundInformation(
                StartTimestamp.ToDateTime().AddMilliseconds(round.TotalMilliseconds()).ToTimestamp(), StartTimestamp,
                out var nextRound);
            await miner.NextRound.SendAsync(nextRound);
        }

        protected async Task NormalBlock(ECKeyPair keyPair)
        {
            var miner = GetConsensusContractTester(keyPair);
            var round = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
            var minerInRound = round.RealTimeMinersInformation[keyPair.PublicKey.ToHex()];
            await miner.UpdateValue.SendAsync(new UpdateValueInput
            {
                OutValue = Hash.Generate(),
                Signature = Hash.Generate(),
                PreviousInValue = minerInRound.PreviousInValue ?? Hash.Empty,
                RoundId = round.RoundId,
                ProducedBlocks = minerInRound.ProducedBlocks + 1,
                ActualMiningTime = minerInRound.ExpectedMiningTime,
                SupposedOrderOfNextRound = 1
            });
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

        #endregion
    }
}