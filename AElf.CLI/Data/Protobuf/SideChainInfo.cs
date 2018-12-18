using System;
using System.Collections.Generic;
using ProtoBuf;

namespace AElf.CLI.Data.Protobuf
{
    [ProtoContract]
    public class SideChainInfo
    {
        [ProtoMember(1)]
        public UInt64 IndexingPrice { get; set; }
        
        [ProtoMember(2)]
        public UInt64 LockedTokenAmount { get; set; }
        
        [ProtoMember(3)]
        public List<ResourceTypeBalancePair> ResourceBalances { get; set; } // this must be set as order of enum ResourceType 
        
        [ProtoMember(4)]
        public byte[] ContractCode { get; set; }
        
        [ProtoMember(5)]
        public Address Proposer { get; set; }
        
        [ProtoMember(6)]
        public SideChainStatus SideChainStatus { get; set; }
    }
    
    [ProtoContract]
    public class ResourceTypeBalancePair{
        
        [ProtoMember(1)]
        public ResourceType Type { get; set;}
        
        [ProtoMember(2)]
        public ulong Amount { get; set; }
    }

    [ProtoContract]
    public enum ResourceType
    {
        [ProtoMember(1)]
        UndefinedResourceType = 0,
        
        [ProtoMember(2)]
        Ram = 1,
        
        [ProtoMember(3)]
        Cpu = 2, 
        
        [ProtoMember(4)]
        Net = 3
    }

    [ProtoContract]
    public enum SideChainStatus
    {
        [ProtoMember(1)]
        Apply = 0,
        [ProtoMember(2)]
        Review = 1,
        [ProtoMember(3)]
        Active = 2,
        [ProtoMember(4)]
        Terminated = 3
    }
}