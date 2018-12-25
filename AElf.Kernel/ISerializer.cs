using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AElf.Kernel
{
    public interface ISerializer<T>
    {
        T Deserialize(byte[] bytes);
        byte[] Serialize(T obj);
    }

    public class Serializer<T> : ISerializer<T>
    {
        private readonly BinaryFormatter _bf=new BinaryFormatter();

        public T Deserialize(byte[] bytes)
        {
            using (var ms = new MemoryStream())
            {
                return (T) _bf.Deserialize(ms);
            }
        }

        public byte[] Serialize(T obj)
        {
            using (var ms = new MemoryStream())
            {
                _bf.Serialize(ms,obj);
                ms.Position = 0;
                return ms.ToArray();
            }
        }
    }
}