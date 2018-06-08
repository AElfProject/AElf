using System;

namespace AElf.Configuration
{
    public interface ISerializer
    {
        string Serialize<T>(T obj);
        
        T Deserialize<T>(string vaule) where T : class;
        
        object Deserialize(string vaule, Type type);
    }
}