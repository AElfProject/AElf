using System;

namespace AElf.Kernel
{
    /// <inheritdoc />
    /// <summary>
    /// Hash result
    /// </summary>
    public interface IHash:IEquatable<IHash>
    {
        byte[] GetBytes();
    }

    public interface IHash<T> : IHash
    {
        
    }
}