using System.Text;
using System.Text.Json.Serialization;
using Google.Protobuf;

namespace Scale;

public class CharType : PrimitiveType<char>
{
    public override string TypeName => "char";

    [JsonIgnore] public override int TypeSize => 1;

    public static explicit operator CharType(char p) => new(p);
    public static implicit operator char(CharType p) => p.Value;

    public CharType()
    {
    }

    public CharType(char value)
    {
        Create(value);
    }

    public override byte[] Encode()
    {
        return Bytes;
    }

    public override void Create(byte[] value)
    {
        Bytes = value;
        Value = Encoding.UTF8.GetString(value)[0];
    }

    public override void Create(char value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public static ByteString GetByteStringFrom(char value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(char value)
    {
        return Encoding.UTF8.GetBytes(value.ToString());
    }

    public static CharType From(char value)
    {
        var instance = new CharType();
        instance.Create(value);
        return instance;
    }
}