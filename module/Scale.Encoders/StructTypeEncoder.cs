namespace Scale.Encoders;

public class StructTypeEncoder
{
    public byte[] Encode(Dictionary<string, object> structFields)
    {
        var byteList = new List<byte>();

        foreach (var field in structFields)
        {
            byte[] fieldBytes = EncodeField(field.Value);
            byteList.AddRange(fieldBytes);
        }

        return byteList.ToArray();
    }

    private byte[] EncodeField(object field)
    {
        switch (field)
        {
            case int intValue:
                return new IntegerTypeEncoder().Encode(intValue);
            case long longValue:
                return new IntegerTypeEncoder().Encode(longValue);
            case uint longValue:
                return new IntegerTypeEncoder().Encode(longValue);
            case ulong longValue:
                return new IntegerTypeEncoder().Encode(longValue);
            case string stringValue:
                return new StringTypeEncoder().Encode(stringValue);
            case byte[] byteArrayValue:
                return byteArrayValue;
            default:
                throw new ArgumentException("Unsupported field type");
        }
    }
}