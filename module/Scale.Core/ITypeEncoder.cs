namespace Scale.Core;

public interface ITypeEncoder
{
    byte[] Encode(object value);
}