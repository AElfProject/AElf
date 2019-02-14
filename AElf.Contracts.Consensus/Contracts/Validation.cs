using AElf.Configuration;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.Contracts
{
    // ReSharper disable UnusedMember.Global
    // ReSharper disable InconsistentNaming
    public class Validation
    {
        private readonly DataCollection _collection;

        public Validation(DataCollection collection)
        {
            _collection = collection;
        }

        public BlockValidationResult ValidateBlock(BlockAbstract blockAbstract)
        {
            var minersList = Api.GetMiners().PublicKeys;
            if (!minersList.Contains(blockAbstract.MinerPublicKey))
            {
                return BlockValidationResult.NotMiner;
            }

            var currentRoundNumber = Api.GetCurrentRoundNumber();
            if (_collection.RoundsMap.TryGet(currentRoundNumber.ToUInt64Value(), out var round))
            {
                var expectedStartMiningTime = round.RealTimeMinersInfo[blockAbstract.MinerPublicKey].ExpectedMiningTime.ToDateTime();
                var expectedStopMiningTime =
                    expectedStartMiningTime.AddMilliseconds(ConsensusConfig.Instance.DPoSMiningInterval);
                if (blockAbstract.Time.ToDateTime() < expectedStartMiningTime ||
                    expectedStopMiningTime < blockAbstract.Time.ToDateTime())
                {
                    return BlockValidationResult.InvalidTimeSlot;
                }
            }

            return BlockValidationResult.Success;
        }
    }
}