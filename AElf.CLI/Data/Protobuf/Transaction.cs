using System;
using System.Collections.Generic;
using AElf.CLI.Wallet.Exceptions;
using AElf.Common;
using Google.Protobuf;
using Newtonsoft.Json.Linq;
using ProtoBuf;

namespace AElf.CLI.Data.Protobuf
{

    /* message Transaction
    {
        Address From = 1;
        Address To = 2;
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
        public Address From { get; set; }
        
        [ProtoMember(2)]
        public Address To { get; set; }

        [ProtoMember(3)]
        public UInt64 RefBlockNumber { get; set; }

        [ProtoMember(4)]
        public byte[] RefBlockPrefix { get; set; }
        
        [ProtoMember(5)]
        public UInt64 IncrementId { get; set; }
        
        [ProtoMember(6)]
        public string MethodName { get; set; }    
        
        [ProtoMember(7)]
        public byte[] Params { get; set; }
        
        [ProtoMember(8)]
        public UInt64 Fee { get; set; }
        
        [ProtoMember(9)]
        public List<byte[]> Sigs { get; set; }
        
        [ProtoMember(10)]
        public TransactionType  Type { get; set; }
        
        
        public static Transaction CreateTransaction(string elementAt, string genesisAddress,
            string methodName, byte[] serializedParams, TransactionType contracttransaction)
        {
            try
            {
                Transaction t = new Transaction();
                t.From = ByteArrayHelpers.FromHexString(elementAt);
                t.To = ByteArrayHelpers.FromHexString(genesisAddress);
                t.MethodName = methodName;
                t.Params = serializedParams;
                t.Type = contracttransaction;
                return t;
            }
            catch (Exception e)
            {
                throw new InvalidTransactionException();
            }
        }
        
        public static Transaction ConvertFromJson(JObject j)
        {
            try
            {
                Transaction tr = new Transaction();
                tr.From = ByteArrayHelpers.FromHexString(j["from"].ToString());
                tr.To = ByteArrayHelpers.FromHexString(j["to"].ToString());
                tr.MethodName = j["method"].ToObject<string>();
                return tr;
            }
            catch (Exception e)
            {
                throw new InvalidTransactionException();
            }
        }
    }

    [ProtoContract]
    public enum TransactionType
    {
        [ProtoMember(1)]
        ContractTransaction = 0,
        
        [ProtoMember(2)]
        DposTransaction = 1,
        
        [ProtoMember(3)]
        CrossChainBlockInfoTransaction = 2,
        
        [ProtoMember(4)]
        MsigTransaction = 3,
        
        [ProtoMember(5)]
        ContractDeployTransaction=4,
    }
}