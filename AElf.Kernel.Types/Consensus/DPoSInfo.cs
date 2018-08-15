using System.Linq;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace AElf.Kernel
{
    //TODO: int -> ulong
    public partial class AElfDPoSInformation
    {
        public Round GetRoundInfo(ulong roundNumber)
        {
            return Rounds[(int) roundNumber - 1];
        }

        public StringValue GetExtraBlockProducerOfSpecificRound(ulong roundNumber)
        {
            return new StringValue {Value = Rounds[(int) roundNumber - 1].BlockProducers.First(bp => bp.Value.IsEBP).Key};
        }

        public Timestamp GetLastBlockProducerTimeslotOfSpecificRound(ulong roundNumber)
        {
            return Rounds[(int) roundNumber - 1].BlockProducers.Last().Value.TimeSlot;
        }
    }
}