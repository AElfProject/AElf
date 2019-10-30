using AElf.Types;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round
    {
        public Round GetUpdateValueRound(string pubkey)
        {
            foreach (var minerInRound in RealTimeMinersInformation.Values)
            {
                minerInRound.ExpectedMiningTime = null;

                minerInRound.Signature = Hash.Empty;
                minerInRound.InValue = Hash.Empty;
                minerInRound.OutValue = Hash.Empty;
                minerInRound.PreviousInValue = Hash.Empty;

                minerInRound.ActualMiningTimes.Clear();
                minerInRound.DecryptedPreviousInValues.Clear();
                minerInRound.EncryptedInValues.Clear();
            }

            return this;
        }

        public Round GetTinyBlockRound(string pubkey)
        {
            return new Round
            {
                RealTimeMinersInformation =
                {
                    [pubkey] = new MinerInRound
                    {
                        ActualMiningTimes = {RealTimeMinersInformation[pubkey].ActualMiningTimes}
                    }
                }
            };
        }
    }
}