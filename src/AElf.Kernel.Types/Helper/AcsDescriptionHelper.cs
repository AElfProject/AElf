using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Reflection;

namespace AElf.Kernel
{
    public static class AcsDescriptionHelper
    {
        public static bool IsAcs(string acsSymbol, IReadOnlyList<ServiceDescriptor> descriptors)
        {
            return descriptors.Any(service => service.File.GetIdentity() == acsSymbol);
        }
    }
}