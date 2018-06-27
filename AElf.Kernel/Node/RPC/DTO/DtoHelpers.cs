using System;
using AElf.Network.Data;
using AElf.Node.RPC.DTO;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace AElf.Kernel.Node.RPC.DTO
{
    public static class DtoHelper
    {

        public static JObject GetTransactionInfo(this ITransaction tx)
        {
            return new JObject {
                ["tx"] = new JObject {
                    {"TxId", tx.GetHash().Value.ToBase64()},
                    {"From", tx.From.Value.ToBase64()},
                    {"To", tx.To.Value.ToBase64()},
                    {"Method", tx.MethodName},
                    {"IncrementId", tx.IncrementId}
                }
            };
        }
        
        
        
        public static TransactionDto ToTransactionDto(this ITransaction tx)
        {
            TransactionDto dto = new TransactionDto()
            {
                Raw = tx.Serialize()
            };

            return dto;
        }

        public static Transaction ToTransaction(this JToken raw)
        {
            var rawData = raw.First.ToString();
            return Transaction.Parser.ParseFrom(ByteString.FromBase64(rawData));
        }

        public static NodeDataDto ToNodeDataDto(this NodeData nd)
        {
            NodeDataDto dto = new NodeDataDto()
            {
                IpAddress = nd.IpAddress,
                Port = Convert.ToUInt16(nd.Port)
            };

            return dto;
        }

        public static NodeData ToNodeData(this NodeDataDto dto)
        {
            NodeData nd = new NodeData()
            {
                IpAddress = dto.IpAddress,
                Port = dto.Port
            };

            return nd;
        }
        
        public static byte[] StringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            
            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            
            return bytes;
        }
    }
}