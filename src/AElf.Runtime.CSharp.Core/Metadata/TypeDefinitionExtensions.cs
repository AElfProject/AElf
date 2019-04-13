using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Metadata
{
    public static class TypeDefinitionExtensions
    {
        public static bool IsSubclassOf(this TypeReference childTypeDef, TypeReference parentTypeDef)
        {
            try
            {
                return childTypeDef.IsSubclassOf(parentTypeDef.Resolve());
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsSubclassOf(this TypeReference childTypeRef, TypeDefinition parentTypeDef)
        {
            var childTypeDef = childTypeRef.Resolve();
            if (childTypeDef == null)
            {
                return false;
            }

            return childTypeDef.IsSubclassOf(parentTypeDef);
        }

        /// <summary>
        /// Is childTypeDef a subclass of parentTypeDef. Does not test interface inheritance
        /// </summary>
        /// <param name="childTypeDef"></param>
        /// <param name="parentTypeDef"></param>
        /// <returns></returns>
        public static bool IsSubclassOf(this TypeDefinition childTypeDef, TypeDefinition parentTypeDef) =>
            childTypeDef.MetadataToken
            != parentTypeDef.MetadataToken
            && childTypeDef
                .EnumerateBaseClasses()
                .Any(b => b.MetadataToken == parentTypeDef.MetadataToken && b.Module == parentTypeDef.Module);

        /// <summary>
        /// Does childType inherit from parentInterface
        /// </summary>
        /// <param name="childType"></param>
        /// <param name="parentInterfaceDef"></param>
        /// <returns></returns>
        public static bool DoesAnySubTypeImplementInterface(this TypeDefinition childType,
            TypeDefinition parentInterfaceDef)
        {
            Debug.Assert(parentInterfaceDef.IsInterface);
            return childType
                .EnumerateBaseClasses()
                .Any(typeDefinition => typeDefinition.DoesSpecificTypeImplementInterface(parentInterfaceDef));
        }

        /// <summary>
        /// Does the childType directly inherit from parentInterface. Base
        /// classes of childType are not tested
        /// </summary>
        /// <param name="childTypeDef"></param>
        /// <param name="parentInterfaceDef"></param>
        /// <returns></returns>
        public static bool DoesSpecificTypeImplementInterface(this TypeDefinition childTypeDef,
            TypeDefinition parentInterfaceDef)
        {
            Debug.Assert(parentInterfaceDef.IsInterface);
            return childTypeDef
                .Interfaces
                .Any(ifaceDef =>
                    DoesSpecificInterfaceImplementInterface(ifaceDef.InterfaceType.Resolve(), parentInterfaceDef));
        }

        /// <summary>
        /// Does interface iface0 equal or implement interface iface1
        /// </summary>
        /// <param name="iface0"></param>
        /// <param name="iface1"></param>
        /// <returns></returns>
        public static bool DoesSpecificInterfaceImplementInterface(TypeDefinition iface0, TypeDefinition iface1)
        {
            Debug.Assert(iface1.IsInterface);
            Debug.Assert(iface0.IsInterface);
            return (iface0.Module == iface1.Module && iface0.MetadataToken == iface1.MetadataToken) ||
                   iface0.DoesAnySubTypeImplementInterface(iface1);
        }


        public static bool IsAssignableFrom(this TypeReference target, TypeReference source)
            => target == source
               || target.MetadataToken == source.MetadataToken
               || target.Resolve().IsAssignableFrom(source.Resolve());

        /// <summary>
        /// Is source type assignable to target type
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsAssignableFrom(this TypeDefinition target, TypeDefinition source)
        {
            if (target == null || source == null)
            {
                return false;
            }
            return target == source
                   || target?.MetadataToken == source?.MetadataToken
                   || source.IsSubclassOf(target)
                   || target.IsInterface && source.DoesAnySubTypeImplementInterface(target);
        }

        /// <summary>
        /// Enumerate the current type, it's parent and all the way to the top type
        /// </summary>
        /// <param name="klassType"></param>
        /// <returns></returns>
        public static IEnumerable<TypeDefinition> EnumerateBaseClasses(this TypeDefinition klassType)
        {
            for (var typeDefinition = klassType;
                typeDefinition != null;
                typeDefinition = typeDefinition.BaseType?.Resolve())
            {
                yield return typeDefinition;
            }
        }

    }
}