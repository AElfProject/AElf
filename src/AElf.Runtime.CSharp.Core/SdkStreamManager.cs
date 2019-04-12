using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace AElf.Runtime.CSharp
{
    public class SdkStreamManager : ISdkStreamManager
    {
        private readonly ConcurrentDictionary<string, MemoryStream> _cachedSdkStreams =
            new ConcurrentDictionary<string, MemoryStream>();

        private readonly string _sdkDir;

        public SdkStreamManager(string sdkDir)
        {
            _sdkDir = sdkDir;
        }

        public Stream GetStream(AssemblyName assemblyName)
        {
            // TODO: Handle version
            var path = Path.Combine(_sdkDir, assemblyName.Name + ".dll");
            if (!File.Exists(path))
            {
                return null;
            }

            if (!_cachedSdkStreams.TryGetValue(path, out var ms))
            {
                var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                ms = new MemoryStream();
                fs.CopyTo(ms);
                _cachedSdkStreams.TryAdd(path, ms);
            }

            ms.Position = 0;
            return ms;
        }
    }
}