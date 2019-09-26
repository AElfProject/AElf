using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AElf.Runtime.CSharp;

namespace AElf.Contracts.Deployer
{
    public static class ContractsDeployer
    {
        public static IReadOnlyDictionary<string, byte[]> GetContractCodes<T>(string contractDir = null)
        {
            var contractNames = GetContractNames(typeof(T).Assembly).ToList();
            if (contractNames.Count == 0)
            {
                throw new NoContractDllFoundInManifestException();
            }

            return contractNames.Select(n => (n, GetCode(n, contractDir))).ToDictionary(x => x.Item1, x => x.Item2);
        }

        private static byte[] GetCode(string dllName,string contractDir)
        {
            var dllPath = Directory.Exists(contractDir)
                ? Path.Combine(contractDir, $"{dllName}.dll")
                : Assembly.Load(dllName).Location;
#if UNIT_TEST
            return File.ReadAllBytes(dllPath);
#else
            return ContractPatcher.Patch(File.ReadAllBytes(dllPath));
#endif
        }

        private static IEnumerable<string> GetContractNames(Assembly assembly)
        {
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