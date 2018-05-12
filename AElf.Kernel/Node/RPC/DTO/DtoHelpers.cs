using System;
using System.Security.Cryptography;
using AElf.Kernel;

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
            Transaction tx = new Transaction
            {
                From = dto.From,
                To = dto.To
            };

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