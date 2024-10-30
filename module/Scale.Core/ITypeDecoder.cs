namespace Scale.Core;

public interface ITypeDecoder
{
    object Decode(byte[] encoded, Type type);
}