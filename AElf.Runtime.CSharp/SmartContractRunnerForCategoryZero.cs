using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.MetaData;
using AElf.Sdk.CSharp;
using AElf.Types.CSharp.MetadataAttribute;
using Google.Protobuf;
using Type = System.Type;
using Module = AElf.Kernel.ABI.Module;

namespace AElf.Runtime.CSharp
{
    public class SmartContractRunnerForCategoryZero : ISmartContractRunner
    {
        public int Category { get; protected set; }
        private readonly ISdkStreamManager _sdkStreamManager;

        private readonly ConcurrentDictionary<string, MemoryStream> _cachedSdkStreams =
            new ConcurrentDictionary<string, MemoryStream>();

        private readonly ConcurrentDictionary<Hash, Type> _cachedContractTypeByHash =
            new ConcurrentDictionary<Hash, Type>();

        private readonly string _sdkDir;
        private readonly AssemblyChecker _assemblyChecker;

        protected readonly IServiceContainer<IExecutivePlugin> _executivePlugins;
        public SmartContractRunnerForCategoryZero(
            string sdkDir,
            IServiceContainer<IExecutivePlugin> executivePlugins,
            IEnumerable<string> blackList = null,
            IEnumerable<string> whiteList = null)
        {
            _sdkDir = Path.GetFullPath(sdkDir);
            _sdkStreamManager = new SdkStreamManager(_sdkDir);
            _assemblyChecker = new AssemblyChecker(blackList, whiteList);
            _executivePlugins = executivePlugins;
        }

        /// <summary>
        /// Creates an isolated context for the smart contract residing with an Api singleton.
        /// </summary>
        /// <returns></returns>
        protected virtual AssemblyLoadContext GetLoadContext()
        {
            // To make sure each smart contract resides in an isolated context with an Api singleton
            return new ContractCodeLoadContext(_sdkStreamManager);
        }

        public virtual async Task<IExecutive> RunAsync(SmartContractRegistration reg)
        {
            var code = reg.Code.ToByteArray();

            var loadContext = GetLoadContext();

            Assembly assembly = null;
            using (Stream stream = new MemoryStream(code))
            {
                assembly = loadContext.LoadFromStream(stream);
            }

            if (assembly == null)
            {
                throw new InvalidCodeException("Invalid binary code.");
            }

            var executive = new Executive(assembly, _executivePlugins);

            return await Task.FromResult(executive);
        }
        
        /// <summary>
        /// Performs code checks.
        /// </summary>
        /// <param name="code">The code to be checked.</param>
        /// <param name="isPrivileged">Is the contract deployed by system user.</param>
        /// <exception cref="InvalidCodeException">Thrown when issues are found in the code.</exception>
        public void CodeCheck(byte[] code, bool isPrivileged)
        {
            _assemblyChecker.CodeCheck(code, isPrivileged);
        }

        #region metadata extraction from contract code

        /// <summary>
        /// 1. extract attributes in type.
        /// 2. check whether this new local calling graph is DAG
        /// 3. Return new class's local function metadata template
        /// </summary>
        /// <param name="contractType">Type of the contract</param>
        /// <exception cref="FunctionMetadataException">Throw when (1) invalid metadata content or (2) find cycles in function call graph</exception>
        /// <returns></returns>
        public ContractMetadataTemplate ExtractMetadata(Type contractType)
        {
            //Extract metadata from code, check validity of existence (whether there is unknown reference and etc.)
            var templateMap = ExtractRawMetadataFromType(contractType, out var contractReferences);

            //before return, calculate the calling graph, check whether there are cycles in local function calls map.
            return new ContractMetadataTemplate(contractType.FullName, templateMap, contractReferences);
        }

        /// <summary>
        /// FunctionMetadataException will be thrown in following cases: 
        /// (1) Duplicate member function name.
        /// (2) Local resource are not declared in the code.
        /// (3) Duplicate smart contract reference name
        /// (4) Duplicate declared field name.
        /// (5) Unknown reference in calling set
        /// </summary>
        /// <param name="contractType"></param>
        /// <param name="contractReferences"></param>
        /// <exception cref="FunctionMetadataException"></exception>
        private Dictionary<string, FunctionMetadataTemplate> ExtractRawMetadataFromType(Type contractType,
            out Dictionary<string, Address> contractReferences)
        {
            var localFunctionMetadataTemplateMap = new Dictionary<string, FunctionMetadataTemplate>();
            var templocalFieldMap = new Dictionary<string, DataAccessMode>();
            contractReferences = new Dictionary<string, Address>();

            //load localFieldMap: <"${this}.[ResourceName]", DataAccessMode>
            foreach (var fieldInfo in contractType.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var fieldAttr = fieldInfo.GetCustomAttribute<SmartContractFieldDataAttribute>();
                if (fieldAttr == null) continue;
                if (!templocalFieldMap.TryAdd(fieldAttr.FieldName, fieldAttr.DataAccessMode))
                {
                    throw new FunctionMetadataException("Duplicate name of field attributes in contract " +
                                                        contractType.FullName);
                }
            }

            //load smartContractReferenceMap: <"[contract_member_name]", Address of the referenced contract>
            foreach (var fieldInfo in contractType.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var smartContractRefAttr = fieldInfo.GetCustomAttribute<SmartContractReferenceAttribute>();
                if (smartContractRefAttr == null) continue;
                try
                {
                    if (!contractReferences.TryAdd(smartContractRefAttr.FieldName,
                        Address.FromString(smartContractRefAttr.ContractAddress)))
                    {
                        throw new FunctionMetadataException(
                            "Duplicate name of smart contract reference attributes in contract " +
                            contractType.FullName);
                    }
                }
                catch (Exception e) when (!(e is FunctionMetadataException))
                {
                    throw new FunctionMetadataException(
                        $"When deploy contract {contractType.FullName}, error occurs where the address {smartContractRefAttr.ContractAddress} of contract reference {smartContractRefAttr.FieldName} is not a valid hex format address ");
                }
            }

            //load localFunctionMetadataTemplateMap: <"${[this]}.FunctionSignature", FunctionMetadataTemplate>
            //FunctionMetadataTemplate: <calling_set, local_resource_set>
            //calling_set: { "${[contract_member_name]}.[FunctionSignature]", ${this}.[FunctionSignature]... }
            //local_resource_set: {"${this}.[ResourceName]"}
            foreach (var methodInfo in contractType.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var functionAttribute = methodInfo.GetCustomAttribute<SmartContractFunctionAttribute>();
                if (functionAttribute == null) continue;

                var resourceSet = functionAttribute.LocalResources.Select(resource =>
                {
                    if (!templocalFieldMap.TryGetValue(resource, out var dataAccessMode))
                    {
                        throw new FunctionMetadataException("Unknown reference local field " + resource +
                                                            " in function " + functionAttribute.FunctionSignature);
                    }

                    return new Resource(resource, dataAccessMode);
                });

                if (!localFunctionMetadataTemplateMap.TryAdd(functionAttribute.FunctionSignature,
                    new FunctionMetadataTemplate(new HashSet<string>(functionAttribute.CallingSet),
                        new HashSet<Resource>(resourceSet))))
                {
                    throw new FunctionMetadataException("Duplicate name of function attribute" +
                                                        functionAttribute.FunctionSignature + " in contract" +
                                                        contractType.FullName);
                }
            }

            if (localFunctionMetadataTemplateMap.Count == 0)
            {
                var blackLists = new[] {"ToString", "Equals", "GetHashCode", "GetType"};
                foreach (var methodInfo in contractType.GetMethods())
                {
                    if (!blackLists.Contains(methodInfo.Name))
                    {
                        localFunctionMetadataTemplateMap.Add("${this}." + methodInfo.Name,
                            new FunctionMetadataTemplate(false));
                    }
                }

                return localFunctionMetadataTemplateMap;
                throw new FunctionMetadataException(
                    "no function marked in the target contract " + contractType.FullName);
            }

            //check for validaty of the calling set (whether have unknow reference)
            foreach (var kvPair in localFunctionMetadataTemplateMap)
            {
                foreach (var calledFunc in kvPair.Value.CallingSet)
                {
                    if (calledFunc.Contains(Replacement.This))
                    {
                        if (!localFunctionMetadataTemplateMap.ContainsKey(calledFunc))
                        {
                            throw new FunctionMetadataException(
                                "calling set of function " + kvPair.Key + " when adding contract " +
                                contractType.FullName + " contains unknown reference to it's own function: " +
                                calledFunc);
                        }
                    }
                    else
                    {
                        if (!Replacement.TryGetReplacementWithIndex(calledFunc, 0, out var memberReplacement) ||
                            !contractReferences.ContainsKey(Replacement.Value(memberReplacement)))
                        {
                            throw new FunctionMetadataException(
                                "calling set of function " + kvPair.Key + " when adding contract " +
                                contractType.FullName + " contains unknown local member reference to other contract: " +
                                calledFunc);
                        }
                    }
                }
            }

            return localFunctionMetadataTemplateMap;
        }

        #endregion
    }
}