using System;

namespace AElf.Kernel
{
    /// <inheritdoc />
    /// <summary>
    /// Hash result
    /// </summary>
    public interface IHash:IEquatable<IHash>
    {
        byte[] GetHashBytes();
    }

    public interface IHash<T> : IHash
    {
        
    }
}