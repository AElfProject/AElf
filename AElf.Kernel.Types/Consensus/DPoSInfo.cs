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