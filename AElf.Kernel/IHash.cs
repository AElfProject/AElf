using System;
using System.Collections.Generic;

namespace AElf.Kernel
{
    /// <inheritdoc />
    /// <summary>
    /// Hash result
    /// </summary>
    public interface IHash : IEquatable<IHash>, IComparer<IHash>
    {
        byte[] Value { get; set; }
        byte[] GetHashBytes();
    }

    public interface IHash<T> : IHash
    {

    }
}