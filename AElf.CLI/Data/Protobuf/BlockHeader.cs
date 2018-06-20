using System;
 using ProtoBuf;
 
 namespace AElf.CLI.Data.Protobuf
 {
     [ProtoContract]
     public class BlockHeader
     {
         [ProtoMember(1)]
         public int Version { get; set; }
         
         [ProtoMember(2)]
         public Hash PreviousBlockHash { get; set; }
         
         [ProtoMember(3)]
         public Hash MerkleTreeRootOfTransactions { get; set; }
         
         [ProtoMember(4)]
         public Hash MerkleTreeRootOfWorldState { get; set; }
         
         [ProtoMember(5)]
         public ulong Index { get; set; }
         
         [ProtoMember(6)]
         public byte[] R { get; set; }
         
         [ProtoMember(7)]
         public byte[] S { get; set; }
         
         [ProtoMember(8)]
         public byte[] P { get; set; }
         
         [ProtoMember(9, DataFormat = DataFormat.WellKnown)]
         public DateTime Time { get; set; }
         
         [ProtoMember(10)]
         public Hash ChainId { get; set; }
     }
 }