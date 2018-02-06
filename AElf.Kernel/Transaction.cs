using System;
using AElf.Kernel.Protobuf;
using Google.Protobuf;

namespace AElf.Kernel
{
    public class Transaction : ITransaction
    {
        private readonly TransactionData _rawTransactionData;

        public Transaction()
        {
        }

        public Transaction(TransactionData rawTransactionData)
        {
            _rawTransactionData = rawTransactionData;
        }

        #region Properties

        /// <summary>
        /// Get and set the name of the smart contract method to be executed
        /// </summary>
        public string MethodName
        {
            get { return _rawTransactionData?.MethodName; }
            set
            {
                if (_rawTransactionData != null)
                    _rawTransactionData.MethodName = value;
            }
        }

        public byte[] From
        {
            get { return _rawTransactionData.From.ToByteArray(); }
            set
            {
                if (_rawTransactionData != null)
                    _rawTransactionData.From = ByteString.CopyFrom(value);
            }
        }

        public byte[] To
        {
            get { return _rawTransactionData.To.ToByteArray(); }
            set
            {
                if (_rawTransactionData != null)
                    _rawTransactionData.To = ByteString.CopyFrom(value);
            }
        }

        public object[] Params { get; set; } // TODO

        public ulong IncrementId { get; set; }

        #endregion

        #region ISerializable

        public byte[] Serialize()
        {
            return _rawTransactionData.ToByteArray();
        }

        #endregion

        #region ITransaction

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

        #endregion
    }
}