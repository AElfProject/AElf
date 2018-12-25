using System;

namespace AElf.Configuration
{
    public interface ISerializer
    {
        string Serialize<T>(T obj);

        T Deserialize<T>(string value) where T : class;

        object Deserialize(string value, Type type);
    }
}