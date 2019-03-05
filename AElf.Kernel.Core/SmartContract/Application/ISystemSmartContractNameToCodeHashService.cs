using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using AElf.Common;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISystemSmartContractNameToCodeHashService
    {
        void Register(string systemSmartContractName, Hash codeHash);

        Hash Get(string systemSmartContractName);
    }


    public static class SystemSmartContractNameToCodeHashServiceExtensions
    {
        public static void Register<T>(
            this ISystemSmartContractNameToCodeHashService systemSmartContractNameToCodeHashService,
            string systemSmartContractName)
        {
            var code = File.ReadAllBytes(typeof(T).Assembly.Location);

            systemSmartContractNameToCodeHashService.Register(systemSmartContractName, Hash.FromRawBytes(code));
        }
    }

    public class SystemSmartContractNameToCodeHashService : ISystemSmartContractNameToCodeHashService,
        ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, Hash> _systemSmartContractNameToCodeHashMap =
            new ConcurrentDictionary<string, Hash>();

        public void Register(string systemSmartContractName, Hash codeHash)
        {
            _systemSmartContractNameToCodeHashMap.TryAdd(systemSmartContractName, codeHash);
        }

        public Hash Get(string systemSmartContractName)
        {
            return _systemSmartContractNameToCodeHashMap.GetOrDefault(systemSmartContractName);
        }
    }
}