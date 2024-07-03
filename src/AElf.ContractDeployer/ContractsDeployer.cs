using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AElf.ContractDeployer;

public static class ContractsDeployer
{
    public static IReadOnlyDictionary<string, byte[]> GetContractCodes<T>(string contractDir = null,
        bool isPatched = false, List<string> pluginContractNames = null)
    {
        var contractNames = GetContractNames(typeof(T).Assembly).ToList();
        if (pluginContractNames is { Count: > 0 })
        {
            contractNames.AddRange(pluginContractNames);
        }

        if (contractNames.Count == 0) throw new NoContractDllFoundInManifestException();

        return contractNames.Select(n => (n, GetCode(n, contractDir, isPatched)))
            .ToDictionary(x => x.Item1, x => x.Item2);
    }

    private static byte[] GetCode(string dllName, string contractDir, bool isPatched)
    {
        var dllPath = Directory.Exists(contractDir)
            ? Path.Combine(contractDir, isPatched ? $"{dllName}.dll.patched" : $"{dllName}.dll")
            : Assembly.Load(dllName).Location;

        return File.ReadAllBytes(dllPath);
    }

    private static IEnumerable<string> GetContractNames(Assembly assembly)
    {
        var manifestName = "Contracts.manifest";

        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(manifestName));
        if (resourceName == default) return new string[0];

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        var result = reader.ReadToEnd();
        return result.Trim().Split('\n').Select(f => f.Trim()).ToArray();
    }
}