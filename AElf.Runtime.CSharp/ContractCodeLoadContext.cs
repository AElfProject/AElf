using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Reflection;
using Akka.Actor;

namespace AElf.Runtime.CSharp
{
    /// <summary>
    /// Smart contract running context which contains the contract assembly with a unique Api singleton.
    /// </summary>
    public class ContractCodeLoadContext : AssemblyLoadContext
    {
        private readonly ConcurrentDictionary<string, MemoryStream> _cachedSdkStreams;
        private readonly string _sdkDir;
        public Assembly Sdk { get; private set; }

        public ContractCodeLoadContext(string sdkDir, ConcurrentDictionary<string, MemoryStream> cachedSdkStreams)
        {
            _sdkDir = sdkDir;
            _cachedSdkStreams = cachedSdkStreams ?? new ConcurrentDictionary<string, MemoryStream>();
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return LoadFromFolderOrDefault(assemblyName);
        }

        private Assembly LoadFromFolderOrDefault(AssemblyName assemblyName)
        {
            if (assemblyName.Name.StartsWith("AElf.Sdk"))
            {
                // API assembly should NOT be shared
                return LoadSdkFromStream(assemblyName);
            }
            return null;
        }

        private Assembly LoadSdkFromStream(AssemblyName assemblyName)
        {
            // TODO: Handle version
            var path = Path.Combine(_sdkDir, assemblyName.Name);
            if (!_cachedSdkStreams.TryGetValue(path, out var ms))
            {
                var fs = new FileStream(path + ".dll", FileMode.Open, FileAccess.Read);
                ms = new MemoryStream();
                fs.CopyTo(ms);
                _cachedSdkStreams.TryAdd(path, ms);
            }

            ms.Position = 0;
            Sdk = LoadFromStream(ms);
            return Sdk;
        }
    }
}