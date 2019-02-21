using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class Contract
    {
        public void InitialTerm(Term term)
        {
            Assert(term.FirstRound.RoundNumber == 1, "It seems that the term number of initial term is incorrect.");
            Assert(term.SecondRound.RoundNumber == 2,
                "It seems that the term number of initial term is incorrect.");
            Process_InitialTerm(term);
        }

        public ActionResult NextTerm(Term term)
        {
            return Process_NextTerm(term);
        }

        public ActionResult SnapshotForMiners(ulong previousTermNumber, ulong lastRoundNumber)
        {
            return Process_SnapshotForMiners(previousTermNumber, lastRoundNumber);
        }

        public ActionResult SnapshotForTerm(ulong snapshotTermNumber, ulong lastRoundNumber)
        {
            return Process_SnapshotForTerm(snapshotTermNumber, lastRoundNumber);
        }

        public ActionResult SendDividends(ulong dividendsTermNumber, ulong lastRoundNumber)
        {
            return Process_SendDividends(dividendsTermNumber, lastRoundNumber);
        }

        public void NextRound(Forwarding forwarding)
        {
            Process_NextRound(forwarding);
        }

        public void PackageOutValue(ToPackage toPackage)
        {
            Process_PackageOutValue(toPackage);
        }

        public void BroadcastInValue(ToBroadcast toBroadcast)
        {
            Process_BroadcastInValue(toBroadcast);
        }
    }
}