using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using AElf.Kernel.SmartContract.Metadata;
using Google.Protobuf.Collections;
using Mono.Cecil;
using Mono.Collections.Generic;
using AccessMode = AElf.Kernel.SmartContract.Metadata.DataAccessPath.Types.AccessMode;

namespace AElf.Runtime.CSharp.Metadata
{
    public abstract class StatePathRetrieverBase
    {
        protected readonly TypeReference _stateType;
        protected readonly GeneralStatePathRetriever _retriever;
        protected readonly ICollection<MethodReference> _methodReferencesInTheSameBody;

        protected IEnumerable<MethodReference> AccessedMethods =>
            _methodReferencesInTheSameBody.Where(IsDeclaredByThisStateType);

        protected StatePathRetrieverBase(TypeReference stateType, ICollection<MethodReference> referencedMethods,
            GeneralStatePathRetriever retriever)
        {
            _stateType = stateType;
            _methodReferencesInTheSameBody = referencedMethods;
            _retriever = retriever;
        }

        public abstract RepeatedField<DataAccessPath> GetPaths();

        private bool IsDeclaredByThisStateType(MethodReference method)
        {
            var declaringType = method.DeclaringType;
            return _stateType.Equals(declaringType) || _stateType.IsSubclassOf(declaringType);
        }
    }

    public class SingletonStatePathRetriever : StatePathRetrieverBase
    {
        private PropertyMethodIndex PropertyMethodIndex => _retriever.PropertyMethodIndex;

        public SingletonStatePathRetriever(TypeReference stateType, ICollection<MethodReference> referencedMethods,
            GeneralStatePathRetriever retriever) : base(stateType, referencedMethods, retriever)
        {
        }

        public RepeatedField<InlineCall> GetInlineCalls()
        {
            if (!IsContractReferenceState())
            {
                return new RepeatedField<InlineCall>();
            }

            var calls = new RepeatedField<InlineCall>();
            var toBeAdded = AccessedMethods.Select(x => PropertyMethodIndex[x.Resolve()])
                .Where(x => x != null)
                .Where(PropertyTypeIsActionType)
                .Select(x => new InlineCall()
                {
                    MethodName = x.Name
                });
            calls.AddRange(toBeAdded);
            return calls;
        }

        public override RepeatedField<DataAccessPath> GetPaths()
        {
            return new RepeatedField<DataAccessPath>()
            {
                new DataAccessPath()
                {
                    Mode = AccessedMethods.Any(x => x.Resolve().IsSetter)
                        ? AccessMode.Write
                        : AccessMode.Read
                }
            };
        }

        #region private methods

        private bool IsContractReferenceState()
        {
            return _retriever.ContractReferenceStateBase.IsAssignableFrom(_stateType);
        }

        private bool PropertyTypeIsActionType(PropertyDefinition property)
        {
            return property.PropertyType.FullName.StartsWith("System.Action");
        }

        #endregion
    }

    public class StructuredStatePathRetriever : StatePathRetrieverBase
    {
        private PropertyMethodIndex PropertyMethodIndex => _retriever.PropertyMethodIndex;

        public StructuredStatePathRetriever(TypeReference stateType, ICollection<MethodReference> referencedMethods,
            GeneralStatePathRetriever retriever) : base(stateType, referencedMethods, retriever)
        {
        }

        public RepeatedField<InlineCall> GetInlineCalls()
        {
            var calls = new RepeatedField<InlineCall>();
            var toBeAdded = AccessedMethods.Select(x => PropertyMethodIndex[x.Resolve()])
                .Where(x => x != null)
                .Where(PropertyTypeIsNotThisStateType)
                .SelectMany(x => GetInlineCallsForOwnedProperty(x).Select(y => y.WithPrefix(x.Name)));
            calls.AddRange(toBeAdded);
            return calls;
        }

        public override RepeatedField<DataAccessPath> GetPaths()
        {
            var paths = new RepeatedField<DataAccessPath>();
            var toBeAdded = AccessedMethods.Select(x => PropertyMethodIndex[x.Resolve()])
                .Where(x => x != null)
                .Where(PropertyTypeIsNotThisStateType)
                .SelectMany(x => GetPathsForOwnedProperty(x).Select(y => y.WithPrefix(x.Name)));
            paths.AddRange(toBeAdded);
            return paths;
        }

        #region private methods

        private bool PropertyTypeIsNotThisStateType(PropertyDefinition property)
        {
            // TODO: Compare TypeReference instead
            return property.PropertyType != _stateType.Resolve();
        }

        private RepeatedField<InlineCall> GetInlineCallsForOwnedProperty(PropertyDefinition propertyDefinition)
        {
            return _retriever.GetInlineCalls(propertyDefinition.PropertyType, _methodReferencesInTheSameBody);
        }

        private RepeatedField<DataAccessPath> GetPathsForOwnedProperty(PropertyDefinition propertyDefinition)
        {
            return _retriever.GetPaths(propertyDefinition.PropertyType, _methodReferencesInTheSameBody);
        }

        #endregion
    }

    public class MappedStatePathRetriever : StatePathRetrieverBase
    {
        private TypeDefinition MappedStateBase => _retriever.MappedStateBase;
        private TypeDefinition AddressType => _retriever.AddressType;

        public MappedStatePathRetriever(TypeReference stateType, ICollection<MethodReference> referencedMethods,
            GeneralStatePathRetriever retriever) : base(stateType, referencedMethods, retriever)
        {
        }


        public override RepeatedField<DataAccessPath> GetPaths()
        {
            var path = new DataAccessPath()
            {
                Mode = AccessedMethods.Any(IsWriteAccessMode) ? AccessMode.Write : AccessMode.Read
            };
            path.Path.AddRange(GetAddressParts(_stateType));
            return new RepeatedField<DataAccessPath>() {path};
        }

        #region private methods

        private TypeReference GetMapType(TypeReference typeReference)
        {
            var type = typeReference;
            var baseType = type.Resolve().BaseType;
            var baseBaseType = baseType.Resolve().BaseType;
            while (!baseBaseType.Equals(MappedStateBase))
            {
                type = baseType;
                baseType = type.Resolve().BaseType;
                baseBaseType = baseType.Resolve().BaseType;
            }

            return type;
        }

        private RepeatedField<string> GetAddressParts(TypeReference typeReference)
        {
            var arguments = (GetMapType(typeReference).Resolve().BaseType as GenericInstanceType)?.GenericArguments ??
                            new Collection<TypeReference>();

            var addresses = new RepeatedField<string>();
            foreach (var a in arguments)
            {
                if (a.Resolve() != AddressType)
                {
                    break;
                }

                addresses.Add($"{{{a.Name}}}");
            }

            return addresses;
        }

        private bool IsWriteAccessMode(MethodReference getterOfSetter)
        {
            var methodDefinition = getterOfSetter.Resolve();
            if (methodDefinition.IsSetter)
            {
                return true;
            }

            var returnType = getterOfSetter.ReturnType;

            if (returnType.IsSubclassOf(MappedStateBase))
            {
                var isDeclaredByReturnType = new Func<MethodReference, bool>(
                    x =>
                        x.DeclaringType.Resolve().Equals(returnType.Resolve()) ||
                        returnType.IsSubclassOf(x.DeclaringType));
                var accessed = _methodReferencesInTheSameBody.Where(isDeclaredByReturnType);
                return accessed.Any(IsWriteAccessMode);
            }

            return false;
        }

        #endregion
    }
}