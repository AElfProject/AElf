using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.WebApp.Application.Chain.Dto
{
    public class MiningSequenceDto
    {
        public string Pubkey { get; set; }
        public Timestamp MiningTime { get; set; }
        public string Behaviour { get; set; }
        public long BlockHeight { get; set; }
        public Hash PreviousBlockHash { get; set; }
    }
}