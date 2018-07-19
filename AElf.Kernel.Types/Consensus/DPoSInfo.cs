using System.Linq;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    //TODO: int -> ulong
    // ReSharper disable once InconsistentNaming
    public partial class DPoSInfo
    {
        public RoundInfo GetRoundInfo(ulong roundNumber)
        {
            return RoundInfo[(int) roundNumber - 1];
        }

        // ReSharper disable once InconsistentNaming
        public BPInfo GetBPInfoOfSpecificRound(ulong roundNumber, string accountAddress)
        {
            return RoundInfo[(int) roundNumber - 1].Info[accountAddress];
        }

        public StringValue GetExtraBlockProducerOfSpecificRound(ulong roundNumber)
        {
            return new StringValue {Value = RoundInfo[(int) roundNumber - 1].Info.First(bp => bp.Value.IsEBP).Key};
        }

        public Timestamp GetLastBlockProducerTimeslotOfSpecificRound(ulong roundNumber)
        {
            return RoundInfo[(int) roundNumber - 1].Info.Last().Value.TimeSlot;
        }
    }
}