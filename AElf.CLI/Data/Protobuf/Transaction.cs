﻿using System;
using ProtoBuf;

namespace AElf.CLI.Data.Protobuf
{

    /* message Transaction
    {
        Hash From = 1;
        Hash To = 2;
        uint64 IncrementId = 3;
        string MethodName = 4;
        bytes Params = 5;
        
        uint64 Fee = 6;
        
        bytes R = 7;
        bytes S = 8;
        bytes P = 9;
    }*/
     
    [ProtoContract]
    public class Transaction
    {
        [ProtoMember(1)]
        public Hash From { get; set; }
        
        [ProtoMember(2)]
        public Hash To { get; set; }
        
        [ProtoMember(3)]
        public UInt64 IncrementId { get; set; }
        
        [ProtoMember(4)]
        public string MethodName { get; set; }
        
        [ProtoMember(5)]
        public byte[] Params { get; set; }
        
        [ProtoMember(6)]
        public UInt64 Fee { get; set; }
        
        [ProtoMember(7)]
        public byte[] R { get; set; }
        
        [ProtoMember(8)]
        public byte[] S { get; set; }
        
        [ProtoMember(9)]
        public byte[] P { get; set; }
    }
}