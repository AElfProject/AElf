using System;

namespace AElf.Serialization
{
    /// <summary>
    /// Represents a serializer in the AElf system.
    /// </summary>
    public interface IAElfSerializer
    {
        byte[] Serialize(object obj);
        
        T Deserialize<T>(byte[] bytes);
        object Deserialize(byte[] bytes, Type type);
    }
}