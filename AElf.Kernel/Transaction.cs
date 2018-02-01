using System;
using AElf.Kernel.Protobuf;
using Google.Protobuf;

namespace AElf.Kernel
{
    public class Transaction : ITransaction, ISerializable
    {
        public Transaction() { }

        /// <summary>
        /// Deserialized data, TransactionData is a Protobuf generated class.
        /// </summary>
        private readonly TransactionData _rawTransactionData;

        public Transaction(TransactionData rawTransactionData)
        {
            // Note that we don't copy the structure, so if you need the original object for later use
            // this can be a problem...
            _rawTransactionData = rawTransactionData;
        }

        /// <summary>
        /// Get and set the value of MethodName in the data structure
        /// </summary>
        public string MethodName
        {
            get { return _rawTransactionData?.MethodName; }
            set
            {
                // validation + logic if needed
                if (_rawTransactionData != null)
                    _rawTransactionData.MethodName = value; // update backing data 
            }
        }
        
        // When we need to get the serialized version of this class
        // we call the protobuf method
        public byte[] Serialize()
        {
            return _rawTransactionData.ToByteArray();
        }
        
        // TODO : Not sure about how to handle this with protobuf
        public object[] Params { get; set; } 

        public IAccount From { get; set; }
        public IAccount To { get; set; }

        public IHash<ITransaction> GetHash()
        {
            return new Hash<ITransaction>(this.GetSHA256Hash());
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
