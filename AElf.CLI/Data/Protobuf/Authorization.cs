using System;
using System.Collections.Generic;
using ProtoBuf;

namespace AElf.CLI.Data.Protobuf
{
    [ProtoContract]
    public class Authorization
    {
        [ProtoMember(1)]
        Address MultiSigAccount { get; set; }
        
        [ProtoMember(2)]
        public UInt32 ExecutionThreshold { get; set; }
        
        [ProtoMember(3)]
        public UInt32 ProposerThreshold { get; set; }
        
        [ProtoMember(4)]
        public List<Reviewer> Reviewers  { get; } = new List<Reviewer>();
    }

    [ProtoContract]
    public class Reviewer
    {
        [ProtoMember(1)]
        public byte[] PubKey { get; set; }
        
        [ProtoMember(2)]
        public UInt32 Weight { get; set; }
    }

    [ProtoContract]
    public class Approval
    {
        [ProtoMember(1)]
        public Hash ProposalHash { get; set; }
        [ProtoMember(2)]
        public byte[] Signature { get; set; }
    }
}