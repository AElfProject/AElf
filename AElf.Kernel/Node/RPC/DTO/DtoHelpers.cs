using System;
using AElf.Kernel;
using AElf.Kernel.Crypto.ECDSA;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Newtonsoft.Json.Linq;
using Parameters = AElf.Kernel.Parameters;

namespace AElf.Node.RPC.DTO
{
    public static class DtoHelper
    {

        public static JObject GetTransactionInfo(this ITransaction tx)
        {
            return new JObject {
                ["tx"] = new JObject {
                    {"txId", tx.GetHash().Value.ToBase64()},
                    {"From", tx.From.Value.ToBase64()},
                    {"To", tx.To.Value.ToBase64()},
                    {"Method", tx.MethodName}
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
            

            //var tx = Transaction.Parser.ParseFrom(dto.Raw);
            // ECKeyPair keyPair = new KeyPairGenerator().Generate();

            /*var tx = new Transaction
            {
                From = Hash.Generate(),
                To = Hash.Generate(),
                IncrementId = 0,
                MethodName = "transfer",
                P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = { new Param
                        {
                            StrVal = "hello"
                        }}
                    }.ToByteArray())
            };

            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
            
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            
            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);*/

            //var tx = Transaction.Parser.ParseFrom(ByteString.FromBase64(@"CiIKIKkqNVMSxCWn/TizqYJl0ymJrnrRqZN+W3incFJX3MRIEiIKIIFxBhlGhI1auR05KafXd/lFGU+apqX96q1YK6aiZLMhIgh0cmFuc2ZlcioJCgcSBWhlbGxvOiEAxfMt77nwSKl/WUg1TmJHfxYVQsygPj0wpZ/Pbv+ZK4pCICzGxsZBCBlASmlDdn0YIv6vRUodJl/9jWd8Q1z2ofFwSkEE+PDQtkHQxvw0txt8bmixMA8lL0VM5ScOYiEI82LX1A6oWUNiLIjwAI0Qh5fgO5g5PerkNebXLPDE2dTzVVyYYw=="));
            var rawData = raw.First.ToString();
            return Transaction.Parser.ParseFrom(ByteString.FromBase64(rawData));
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