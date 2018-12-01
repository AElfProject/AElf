using System;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf;

namespace AElf.CLI.Data.Protobuf
{
    [ProtoContract]
    public class Proposal
    {
        [ProtoMember(1)]
        public Address MultiSigAccount { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public PendingTxn TxnData { get; set; }
        
        [ProtoMember(4, DataFormat = DataFormat.WellKnown)]
        public DateTime ExpiredTime { get; set; }
        
        [ProtoMember(5)]
        public ProposalStatus Status { get; set; }
        
        [ProtoMember(6)]
        public Address Proposer { get; set; }
    }

    [ProtoContract]
    public class PendingTxn
    {
        [ProtoMember(1)]
        public string ProposalName { get; set; }

        [ProtoMember(2)]
        public byte[] TxnData { get; set; }
    }
    
    [ProtoContract]
    public enum ProposalStatus {
        
        [ProtoMember(1)]
        ToBeDecided = 0,
        
        [ProtoMember(2)]
        Decided = 1,
        
        [ProtoMember(3)]
        Released = 2
    }
}