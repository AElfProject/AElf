using Google.Protobuf.Reflection;

namespace AElf.Types.CSharp
{
    public static class FileDescriptorExtensions
    {
        public static string GetIndentity(this FileDescriptor descriptor)
        {
            if (descriptor.CustomOptions.TryGetString(500001, out var id))
            {
                return id;
            }

            return "";
        }
    }
}