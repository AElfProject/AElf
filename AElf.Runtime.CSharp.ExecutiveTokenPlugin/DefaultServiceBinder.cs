using System.Collections.Generic;
using AElf.Types.CSharp;
using Google.Protobuf.Reflection;

namespace AElf.Runtime.CSharp.ExecutiveTokenPlugin
{
    internal class DefaultServiceBinder : ServiceBinderBase
    {
        internal readonly List<ServiceDescriptor> descriptors = new List<ServiceDescriptor>();

        public override void AddDescriptor(ServiceDescriptor descriptor)
        {
            descriptors.Add(descriptor);
        }

        internal IReadOnlyList<ServiceDescriptor> GetDescriptors()
        {
            return descriptors.AsReadOnly();
        }
    }
}