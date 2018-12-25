namespace AElf.Common.Serializers
{
    public interface IByteSerializer
    {
        byte[] Serialize(object obj);
        T Deserialize<T>(byte[] bytes);
    }
}