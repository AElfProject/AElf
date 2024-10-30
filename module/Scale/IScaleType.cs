namespace Scale;

public interface IScaleType
{
    string TypeName { get; }
    
    int TypeSize { get; }

    byte[] Encode();

    void Decode(byte[] value, ref int p);

    void Create(string value);
    void Create(byte[] value);
    
    
}