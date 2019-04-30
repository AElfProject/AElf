using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Kernel.SmartContract.Metadata;
using Google.Protobuf.Collections;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.Runtime.CSharp.Metadata
{
    public class GeneralStatePathRetriever : IStatePathRetrieverForModule
    {
        private readonly Dictionary<MethodDefinition, RepeatedField<DataAccessPath>> _cache =
            new Dictionary<MethodDefinition, RepeatedField<DataAccessPath>>();

        #region properties

        public PropertyMethodIndex PropertyMethodIndex { get; private set; }

        #region modules

        public ModuleDefinition Sdk { get; private set; }
        public ModuleDefinition AelfCommon { get; private set; }
        public ModuleDefinition Module { get; private set; }

        #endregion

        #region types

        public TypeDefinition AddressType { get; private set; }

        #region states

        public TypeDefinition StateBase { get; private set; }
        public TypeDefinition SingletonStateBase { get; private set; }
        public TypeDefinition StructuredStateBase { get; private set; }
        public TypeDefinition MappedStateBase { get; private set; }
        public TypeDefinition ContractReferenceStateBase { get; private set; }

        #endregion

        #region contract related

        // ReSharper disable once InconsistentNaming
        public TypeDefinition IContext { get; private set; }

        public TypeDefinition ContractStateBase { get; private set; }

        public TypeDefinition ContractBase { get; private set; }
        public TypeDefinition ContractType => Module.Types.Single(x => x.IsSubclassOf(ContractBase));
        public TypeDefinition ContractState => Module.Types.Single(x => x.IsSubclassOf(ContractStateBase));

        #endregion

        #endregion

        #endregion

        public GeneralStatePathRetriever(IAssemblyResolver resolver, Stream stream)
        {
            Module = ModuleDefinition.ReadModule(stream, new ReaderParameters()
            {
                AssemblyResolver = resolver
            });
            var sdkName = Module.AssemblyReferences.Single(a => a.Name.Split(",")[0] == "AElf.Sdk.CSharp");
            Sdk = resolver.Resolve(sdkName).MainModule;
            var aelfCommonName = Module.AssemblyReferences.Single(a => a.Name.Split(",")[0] == "AElf.Common");
            AelfCommon = resolver.Resolve(aelfCommonName).MainModule;
            AddressType = AelfCommon.GetType("AElf.Common.Address");
            IContext = Sdk.GetType("AElf.Sdk.CSharp.IContext");
            StateBase = Sdk.GetType("AElf.Sdk.CSharp.State.StateBase");
            ContractStateBase = Sdk.GetType("AElf.Sdk.CSharp.State.ContractState");
            ContractBase = Sdk.GetType("AElf.Sdk.CSharp.CSharpSmartContract");
            SingletonStateBase = Sdk.GetType("AElf.Sdk.CSharp.State.SingletonState");
            StructuredStateBase = Sdk.GetType("AElf.Sdk.CSharp.State.StructuredState");
            MappedStateBase = Sdk.GetType("AElf.Sdk.CSharp.State.MappedStateBase");
            ContractReferenceStateBase = Sdk.GetType("AElf.Sdk.CSharp.State.ContractReferenceState");
            PropertyMethodIndex = new PropertyMethodIndex(Module.Types.Where(x => x.IsSubclassOf(StateBase))
                .SelectMany(s => s.Properties));
        }

        #region IStatePathRetrieverForModule

        public Dictionary<MethodDefinition, RepeatedField<DataAccessPath>> GetPaths()
        {
            return ContractType.Methods.ToDictionary(m => m, m => GetPaths(m));
        }

        public Dictionary<MethodDefinition, RepeatedField<InlineCall>> GetInlineCalls()
        {
            return ContractType.Methods.ToDictionary(m => m, m => GetInlineCalls(m));
        }

        #endregion

        public RepeatedField<InlineCall> GetInlineCalls(TypeReference stateType,
            ICollection<MethodReference> referencedMethods)
        {
            if (stateType.IsSubclassOf(SingletonStateBase))
            {
                return new SingletonStatePathRetriever(stateType, referencedMethods, this)
                    .GetInlineCalls();
            }

            if (stateType.IsSubclassOf(StructuredStateBase))
            {
                return new StructuredStatePathRetriever(stateType, referencedMethods, this)
                    .GetInlineCalls();
            }

            if (stateType.IsSubclassOf(MappedStateBase))
            {
                return new RepeatedField<InlineCall>();
            }

            throw new Exception("Unable to identify state type.");
        }

        public RepeatedField<DataAccessPath> GetPaths(TypeReference stateType,
            ICollection<MethodReference> referencedMethods)
        {
            if (stateType.IsSubclassOf(SingletonStateBase))
            {
                return new SingletonStatePathRetriever(stateType, referencedMethods, this)
                    .GetPaths();
            }

            if (stateType.IsSubclassOf(StructuredStateBase))
            {
                return new StructuredStatePathRetriever(stateType, referencedMethods, this)
                    .GetPaths();
            }

            if (stateType.IsSubclassOf(MappedStateBase))
            {
                return new MappedStatePathRetriever(stateType, referencedMethods, this)
                    .GetPaths();
            }

            throw new Exception("Unable to identify state type.");
        }

        private RepeatedField<InlineCall> GetInlineCalls(MethodDefinition method, HashSet<MethodDefinition> seen = null)
        {
            AssertDetectable(method);
            seen = seen ?? new HashSet<MethodDefinition>();
            seen.Add(method);

            var references = new HashSet<MethodReference>(GetReferencedMethods(method));
            var calls = GetInlineCalls(ContractState, references);

            var referencedOtherMethodsInContractType = references.Where(DeclaredByContractType)
                .Select(x => x.Resolve()).Where(definition => !seen.Contains(definition));
            foreach (var definition in referencedOtherMethodsInContractType)
            {
                calls.AddRange(GetInlineCalls(definition, seen));
            }

            return new RepeatedField<InlineCall>() {calls.Distinct()};
        }

        private RepeatedField<DataAccessPath> GetPaths(MethodDefinition method, HashSet<MethodDefinition> seen = null)
        {
            AssertDetectable(method);
            seen = seen ?? new HashSet<MethodDefinition>();
            seen.Add(method);

            if (_cache.TryGetValue(method, out var paths))
            {
                return paths;
            }

            var references = new HashSet<MethodReference>(GetReferencedMethods(method));
            paths = GetPaths(ContractState, references);

            var referencedOtherMethodsInContractType = references.Where(DeclaredByContractType)
                .Select(x => x.Resolve()).Where(definition => !seen.Contains(definition));
            foreach (var definition in referencedOtherMethodsInContractType)
            {
                paths.AddRange(GetPaths(definition, seen));
            }

            return _cache[method] = new RepeatedField<DataAccessPath>() {paths.Distinct()};
        }

        #region private methods

        private void AssertDetectable(MethodDefinition method)
        {
            var isGetterOrSetter = new Func<MethodReference, bool>(m =>
            {
                var definition = m.Resolve();
                return definition.IsGetter || definition.IsSetter;
            });
            foreach (var reference in GetReferencedMethods(method))
            {
                if (DeclaredByStateType(reference))
                {
                    // Method declared in State Type
                    // Only property setter and getter are allowed
                    if (!isGetterOrSetter(reference))
                    {
                        throw new StateTypeDeclaringNonPropertyMethodException(reference);
                    }
                }
                else if (!DeclaredByContractType(reference))
                {
                    // Method declared in Non-State-and-Contract Type
                    // Cannot operate State Types
                    if (OperatingOnState(reference))
                    {
                        throw new StateAccessedInNonStateOrContractTypeException(reference);
                    }
                }
                // TODO: Maybe check generic parameters

                // Only contract methods can access state
            }
        }

        private bool DeclaredByStateType(MethodReference method)
        {
            return StateBase.IsAssignableFrom(method.DeclaringType);
        }

        private bool DeclaredByContractType(MethodReference method)
        {
            return ContractBase.IsAssignableFrom(method.DeclaringType);
        }

        private bool OperatingOnState(MethodReference method, HashSet<MethodReference> seen = null)
        {
            seen = seen ?? new HashSet<MethodReference>();
            // add self in seen
            seen.Add(method);
            var references = GetReferencedMethods(method.Resolve()).Where(x => !seen.Contains(x)).ToList();
            return references.Any(DeclaredByStateType) || references.Any(r => OperatingOnState(r, seen));
        }

        private static IEnumerable<MethodReference> GetReferencedMethods(MethodDefinition method)
        {
            // TODO: Handle interface
            if (!method.HasBody) yield break;
            foreach (var ins in method.Body.Instructions)
            {
                if (ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt)
                {
                    yield return (MethodReference) ins.Operand;
                }
            }
        }

        #endregion
    }
}