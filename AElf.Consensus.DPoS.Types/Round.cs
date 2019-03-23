using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Consensus.DPoS
{
    public partial class Round
    {
        public long RoundId =>
            RealTimeMinersInformation.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds).Sum();

        public Hash GetHash(bool isContainPreviousInValue = true)
        {
            return Hash.FromRawBytes(GetCheckableRound(isContainPreviousInValue));
        }

        private byte[] GetCheckableRound(bool isContainPreviousInValue = true)
        {
            var minersInformation = new Dictionary<string, MinerInRound>();
            foreach (var minerInRound in RealTimeMinersInformation)
            {
                var checkableMinerInRound = new MinerInRound
                {
                    PublicKey = minerInRound.Value.PublicKey, OutValue = minerInRound.Value.OutValue,
                    IsExtraBlockProducer = minerInRound.Value.IsExtraBlockProducer,
                    Signature = minerInRound.Value.Signature, Order = minerInRound.Value.Order,
                    ExpectedMiningTime = minerInRound.Value.ExpectedMiningTime,
                    PromisedTinyBlocks = minerInRound.Value.PromisedTinyBlocks,
                    OrderOfNextRound = minerInRound.Value.OrderOfNextRound
                };
                if (isContainPreviousInValue)
                {
                    checkableMinerInRound.PreviousInValue = minerInRound.Value.PreviousInValue;
                }
                minersInformation.Add(minerInRound.Key, checkableMinerInRound);
            }

            var checkableRound = new Round
            {
                RoundNumber = RoundNumber,
                TermNumber = TermNumber,
                RealTimeMinersInformation = {minersInformation},
                BlockchainAge = BlockchainAge
            };
            return checkableRound.ToByteArray();
        }
    }
}