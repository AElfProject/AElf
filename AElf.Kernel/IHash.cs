using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.Kernel
{
    /// <inheritdoc cref="" />
    /// <summary>
    /// Hash result
    /// </summary>
    public interface IHash : IEquatable<IHash>, IComparer<IHash>, IMessage
    {
        ByteString Value { get; set; }
    }

}