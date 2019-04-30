﻿using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class Block : ICustomDiagnosticMessage, IBlock
    {
        /// <summary>
        /// Used to override IMessage's default string representation.
        /// </summary>
        /// <returns></returns>
        public string ToDiagnosticString()
        {
            return $"{{ id: {GetHash()}, height: {Height} }}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AElf.Kernel.Block"/> class.
        /// A previous block must be referred, except for the genesis block.
        /// </summary>
        /// <param name="preBlockHash">Pre block hash.</param>
        public Block(Hash preBlockHash)
        {
            Header = new BlockHeader(preBlockHash);
            Body = new BlockBody();
        }

        public long Height
        {
            get => Header?.Height ?? 0;
            set { }
        }

        public Hash GetHash()
        {
            return Header.GetHash();
        }

        public byte[] GetHashBytes()
        {
            return Header.GetHashBytes();
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }

        public IEnumerable<Transaction> TransactionList => Body.TransactionList;
    }
}