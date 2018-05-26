using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Reflection;

namespace AElf.Runtime.CSharp
{
    /// <summary>
    /// Smart contract running context which contains the contract assembly with a unique Api singleton.
    /// </summary>
    public class CSharpAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _apiDllDirectory;
        private readonly Assembly[] _parentLoaded;

        public CSharpAssemblyLoadContext(string apiDllDirectory, Assembly[] parentLoaded)
        {
            _apiDllDirectory = apiDllDirectory;
            _parentLoaded = parentLoaded;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return LoadFromFolderOrDefault(assemblyName);
        }

        Assembly LoadFromFolderOrDefault(AssemblyName assemblyName)
        {
            // API assembly should NOT be shared
            var path = Path.Combine(_apiDllDirectory, assemblyName.Name);

            if (File.Exists(path + ".dll"))
                return LoadFromAssemblyPath(path + ".dll");
            
            return _parentLoaded.FirstOrDefault(x => x.GetName().Name == assemblyName.Name);
        }
    }
}