using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AElf.Contracts.Deployer
{
    public static class ContractsManifest
    {
        public static IEnumerable<string> GetContractNames()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var manifestName = "Contracts.manifest";

            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(manifestName));
            if (resourceName == default(string))
            {
                return new string[0];
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var result = reader.ReadToEnd();
                return result.Trim().Split('\n').Select(f => f.Trim()).ToArray();
            }
        }
    }
}