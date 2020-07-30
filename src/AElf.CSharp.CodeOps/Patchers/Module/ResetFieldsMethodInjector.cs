using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.CSharp.CodeOps.Patchers.Module
{
    public class ResetFieldsMethodInjector : IPatcher<ModuleDefinition>
    {
        public bool SystemContactIgnored => false;
        
        public void Patch(ModuleDefinition module)
        {
            foreach (var type in module.Types.Where(t => !t.IsContractImplementation()))
            {
                InjectTypeWithResetFields(module, type);
                AddCallToNestedClassResetFields(type);
            }

            InjectContractWithResetFields(module);
        }

        private bool InjectTypeWithResetFields(ModuleDefinition module, TypeDefinition type)
        {
            var callToNestedResetNeeded = false;

            // Inject for nested types first
            foreach (var nestedType in type.NestedTypes)
            {
                callToNestedResetNeeded |= InjectTypeWithResetFields(module, nestedType);
            }
            
            // Get static non-initonly, non-constant fields
            var fields = type.Fields.Where(f => 
                f.FieldType.FullName != typeof(FileDescriptor).FullName && 
                f.IsStatic && 
                !(f.IsInitOnly || f.HasConstant)
                ).ToList();

            callToNestedResetNeeded |= fields.Any();

            if (callToNestedResetNeeded)
            {
                // Inject ResetFields method in type, so that nested calls can be added later
                type.Methods.Add(ConstructResetFieldsMethod(module, fields, false));
            }

            return callToNestedResetNeeded;
        }

        private void AddCallToNestedClassResetFields(TypeDefinition type)
        {
            var parentClassResetMethod = type.Methods.FirstOrDefault(m => m.Name == nameof(ResetFields));

            if (parentClassResetMethod == null)
                return;
            
            var il = parentClassResetMethod.Body.GetILProcessor();

            var ret = il.Body.Instructions.Last();

            // Get reference of the methods to nested types' reset fields methods
            var toBeCalledFromParent = type.NestedTypes
                .Where(t => t.Methods.Any(m => m.Name == nameof(ResetFields)))
                .Select(t => t.Methods.Single(m => m.Name == nameof(ResetFields)));
            
            foreach (var nestedResetMethod in toBeCalledFromParent)
            {
                // Add a call to nested class' ResetFields method
                il.InsertBefore(ret, il.Create(OpCodes.Call, nestedResetMethod));
            }

            // Do the same for nested types (add call to their nested types' ResetMethod)
            foreach (var nestedType in type.NestedTypes)
            {
                AddCallToNestedClassResetFields(nestedType);
            }
        }

        private void InjectContractWithResetFields(ModuleDefinition module)
        {
            var contractImplementation = module.Types.Single(t => t.IsContractImplementation());
            
            foreach (var nestedType in contractImplementation.NestedTypes)
            {
                InjectTypeWithResetFields(module, nestedType);
                AddCallToNestedClassResetFields(nestedType);
            }
            
            var otherTypes = module.Types.Where(t => !t.IsContractImplementation()).ToList();

            // Add contract's nested types as well
            otherTypes.AddRange(contractImplementation.NestedTypes);
            
            // Get all fields, including instance fields from the base type of the inherited contract type
            var resetFieldsMethod = ConstructResetFieldsMethod(module, 
                contractImplementation.GetAllFields(f=>!(f.IsInitOnly || f.HasConstant)), true);

            var il = resetFieldsMethod.Body.GetILProcessor();

            var ret = il.Body.Instructions.Last();

            // Call other types' reset fields method from contract implementation reset method
            foreach (var resetMethodRef in otherTypes.Select(type => 
                type.Methods.FirstOrDefault(m => 
                    m.Name == nameof(ResetFields))).Where(resetMethodRef => resetMethodRef != null))
            {
                il.InsertBefore(ret, il.Create(OpCodes.Call, resetMethodRef));
            }
            
            contractImplementation.Methods.Add(resetFieldsMethod);
        }

        private MethodDefinition ConstructResetFieldsMethod(ModuleDefinition module, IEnumerable<FieldDefinition> fields, bool instanceMethod)
        {
            var attributes = instanceMethod
                ? MethodAttributes.Public | MethodAttributes.HideBySig
                : MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static;
            
            var resetFieldsMethod = new MethodDefinition(
                nameof(ResetFields), 
                attributes, 
                module.ImportReference(typeof(void))
            );
            
            resetFieldsMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(bool))));
            var il = resetFieldsMethod.Body.GetILProcessor();

            foreach (var field in fields)
            {
                if (instanceMethod && !field.IsStatic)
                    il.Emit(OpCodes.Ldarg_0); // this
                LoadDefaultValue(il, field);
                il.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
            }

            il.Emit(OpCodes.Ret);

            return resetFieldsMethod;
        }

        private static void LoadDefaultValue(ILProcessor il, FieldReference field)
        {
            il.Emit(field.FieldType.IsValueType ? OpCodes.Ldc_I4_0 : OpCodes.Ldnull);
        }

        public static void ResetFields()
        {
            // This method is to use its name only
        }
    }
}