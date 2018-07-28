using System;
using AElf.Common.ByteArrayHelpers;
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
            return new JObject
            {
                ["tx"] = new JObject
                {
                    {"TxId", tx.GetHash().ToHex()},
                    {"From", tx.From.ToHex()},
                    {"To", tx.To.ToHex()},
                    {"Method", tx.MethodName},
                    {"IncrementId", tx.IncrementId}
                }
            };
        }

        public static TransactionDto ToTransactionDto(this ITransaction tx)
        {
            var transactionDto = new TransactionDto
            {
                Raw = tx.Serialize()
            };

            return transactionDto;
        }

        public static Transaction ToTransaction(this JToken raw)
        {
            var rawData = raw.First.ToString();
            return Transaction.Parser.ParseFrom(ByteString.CopyFrom(ByteArrayHelpers.FromHexString(rawData)));
        }

        public static NodeDataDto ToNodeDataDto(this NodeData nd)
        {
            var nodeDataDto = new NodeDataDto
            {
                IpAddress = nd.IpAddress,
                Port = Convert.ToUInt16(nd.Port)
            };

            return nodeDataDto;
        }

        public static NodeData ToNodeData(this NodeDataDto dto)
        {
            var nodeData = new NodeData
            {
                IpAddress = dto.IpAddress,
                Port = dto.Port
            };

            return nodeData;
        }

        public static byte[] StringToByteArray(string hex)
        {
            var numberChars = hex.Length;
            var bytes = new byte[numberChars / 2];

            for (var i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        }
    }
}