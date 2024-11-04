using Google.Protobuf;
using Google.Protobuf.Collections;

namespace AElf
{
    public static class ProtoExtensions
    {
        private static void CopyNonNullProperties<T>(T source, T target)
        {
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                if (!property.CanRead || !property.CanWrite) continue;
                var value = property.GetValue(source);
                if (value != null)
                {
                    property.SetValue(target, value);
                }
            }
        }

        public static T MergeFromIndexed<T>(RepeatedField<ByteString> indexed) where T : IMessage<T>, new()
        {
            var target = new T();
            foreach (var byteString in indexed)
            {
                var parsedInstance = new T();
                parsedInstance.MergeFrom(byteString);
                CopyNonNullProperties(parsedInstance, target);
            }

            return target;
        }
    }
}