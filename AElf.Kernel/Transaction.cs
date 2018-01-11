using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel
{
    public class Transaction : ITransaction
    {
        public Transaction() { }

        public IHash<ITransaction> GetHash()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, this);
                return new Hash<ITransaction>(SHA256.Create().ComputeHash(ms));
            }
        }

        public ITransactionParallelMetaData GetParallelMetaData()
        {
            throw new NotImplementedException();
        }

        public IHash<IBlockHeader> LastBlockHashWhenCreating()
        {
            throw new NotImplementedException();
        }
    }
}
