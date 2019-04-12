using System.IO;
using System.Reflection;

namespace AElf.Runtime.CSharp
{
    public interface ISdkStreamManager
    {
        Stream GetStream(AssemblyName assemblyName);
    }
}