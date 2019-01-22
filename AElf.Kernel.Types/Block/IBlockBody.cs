using System.Collections.Generic;
using Google.Protobuf.Collections;
using AElf.Common;

namespace AElf.Kernel
{
    public interface IBlockBody: IHashProvider
    {
        RepeatedField<Hash> Transactions { get; }        
    }
}