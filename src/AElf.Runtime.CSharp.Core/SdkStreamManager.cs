using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace AElf.Runtime.CSharp
{
    public class SdkStreamManager : ISdkStreamManager
    {
        private readonly ConcurrentDictionary<string, byte[]> _cachedSdkStreams =
            new ConcurrentDictionary<string, byte[]>();

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
                var assembly = Assembly.Load(assemblyName);

                if (assembly == null)
                    return null;

                path = assembly.Location;
            }

            if (!_cachedSdkStreams.TryGetValue(path, out var buffer))
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var length = (int)fs.Length;
                    buffer = new byte[length];
                    fs.Read(buffer, 0, length);
                    _cachedSdkStreams.TryAdd(path, buffer);
                }
            }

            return new MemoryStream(buffer);
        }
    }
}