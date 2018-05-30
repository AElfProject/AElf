using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Crypto.ECDSA;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Parameters = AElf.Kernel.Parameters;

namespace AElf.Node.RPC.DTO
{
    public static class DtoHelper
    {
        public static TransactionDto ToTransactionDto(this ITransaction tx)
        {
            TransactionDto dto = new TransactionDto()
            {
                From = tx.From.GetHashBytes(),
                To = tx.To.GetHashBytes()
            };

            return dto;
        }

        public static Transaction ToTransaction(this TransactionDto dto)
        {
            var parameters = new Parameters();
            foreach (var param in dto.Params)
            {
                parameters.Params.Add(param.ToParam());
            }
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();

            var tx = new Transaction
            {
                From = dto.From,
                To = dto.To,
                IncrementId = dto.IncrementId,
                MethodName = dto.Method,
                P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded()),
                Params = parameters.ToByteString()
            };

            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
            
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            
            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);
            return tx;
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