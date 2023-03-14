using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.SmartContract;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps.Patchers.Module;

public class ExecutionObserverInjector : IPatcher<ModuleDefinition>
{
    public bool SystemContactIgnored => true;

    public void Patch(ModuleDefinition module)
    {
        // Check if already injected, do not double inject
        if (module.Types.Select(t => t.Name).Contains(nameof(ExecutionObserverProxy)))
            return;

        // ReSharper disable once IdentifierTypo
        var nmspace = module.Types.Single(m => m.BaseType is TypeDefinition).Namespace;

        var proxyBuilder = new ExecutionObserverProxyBuilder(module, nmspace);

        foreach (var method in module.GetAllTypes().SelectMany(t => t.Methods))
        {
            new MethodPatcher(method, proxyBuilder).DoPatch();
        }

        module.Types.Add(proxyBuilder.ObserverType);
    }
}

class ExecutionObserverProxyBuilder
{
    private readonly ModuleDefinition _hostModule;
    private readonly string _namespace;
    private TypeDefinition _observerType;
    private FieldDefinition _observerField;
    private MethodDefinition _setObserverMethod;
    private MethodDefinition _branchCountMethod;
    private MethodDefinition _callCountMethod;

    internal ExecutionObserverProxyBuilder([NotNull] ModuleDefinition hostModule, [NotNull] string namespace_)
    {
        _hostModule = hostModule ?? throw new ArgumentNullException(nameof(hostModule));
        _namespace = namespace_ ?? throw new ArgumentNullException(nameof(namespace_));
    }

    public TypeDefinition ObserverType
    {
        get
        {
            if (_observerType == null)
            {
                _observerType = new TypeDefinition(
                    _namespace,
                    nameof(ExecutionObserverProxy),
                    TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.Public | TypeAttributes.Class,
                    _hostModule.ImportReference(typeof(object))
                );
                _observerType.Fields.Add(ObserverField);
                _observerType.Methods.Add(SetObserverMethod);
                _observerType.Methods.Add(BranchCountMethod);
                _observerType.Methods.Add(CallCountMethod);
            }

            return _observerType;
        }
    }

    /// <summary>
    ///     [ThreadStatic] private static IExecutionObserver _observer;
    /// </summary>
    private FieldDefinition ObserverField
    {
        get
        {
            if (_observerField == null)
            {
                _observerField = new FieldDefinition(
                    "_observer",
                    FieldAttributes.Private | FieldAttributes.Static,
                    _hostModule.ImportReference(typeof(IExecutionObserver)
                    )
                );
                _observerField.CustomAttributes.Add(
                    new CustomAttribute(
                        _hostModule.ImportReference(typeof(ThreadStaticAttribute).GetConstructor(new Type[] { })))
                );
            }

            return _observerField;
        }
    }

    /// <summary>
    /// public static void SetObserver([In] IExecutionObserver observer) => ExecutionObserverProxy._observer = observer;
    /// </summary>
    private MethodDefinition SetObserverMethod
    {
        get
        {
            if (_setObserverMethod == null)
            {
                _setObserverMethod = new MethodDefinition(
                    nameof(ExecutionObserverProxy.SetObserver),
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                    _hostModule.ImportReference(typeof(void))
                );

                _setObserverMethod.Parameters.Add(
                    new ParameterDefinition(
                        "observer",
                        ParameterAttributes.In,
                        _hostModule.ImportReference(typeof(IExecutionObserver))
                    )
                );

                var il = _setObserverMethod.Body.GetILProcessor();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Stsfld, ObserverField);
                il.Emit(OpCodes.Ret);
            }

            return _setObserverMethod;
        }
    }

    /// <summary>
    /// public static void BranchCount(){ if (ExecutionObserverProxy._observer == null) return; ExecutionObserverProxy._observer.BranchCount(); } 
    /// </summary>
    internal MethodDefinition BranchCountMethod
    {
        get
        {
            if (_branchCountMethod == null)
            {
                _branchCountMethod = new MethodDefinition(
                    nameof(ExecutionObserverProxy.BranchCount),
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                    _hostModule.ImportReference(typeof(void))
                );

                _branchCountMethod.Body.Variables.Add(
                    new VariableDefinition(_hostModule.ImportReference(typeof(bool)))
                );
                var il = _branchCountMethod.Body.GetILProcessor();

                var ret = il.Create(OpCodes.Ret);

                il.Emit(OpCodes.Ldsfld, ObserverField);
                il.Emit(OpCodes.Brfalse_S, ret); // Do not call if not initialized
                il.Emit(OpCodes.Ldsfld, ObserverField);
                il.Emit(
                    OpCodes.Callvirt,
                    _hostModule.ImportReference(
                        typeof(IExecutionObserver).GetMethod(nameof(IExecutionObserver.BranchCount))
                    )
                );
                il.Append(ret);
            }

            return _branchCountMethod;
        }
    }

    /// <summary>
    /// public static void CallCount() { if (ExecutionObserverProxy._observer == null) return; ExecutionObserverProxy._observer.CallCount(); }
    /// </summary>
    internal MethodDefinition CallCountMethod
    {
        get
        {
            if (_callCountMethod == null)
            {
                _callCountMethod = new MethodDefinition(
                    nameof(ExecutionObserverProxy.CallCount),
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                    _hostModule.ImportReference(typeof(void))
                );

                _callCountMethod.Body.Variables.Add(new VariableDefinition(_hostModule.ImportReference(typeof(bool))));
                var il = _callCountMethod.Body.GetILProcessor();

                var ret = il.Create(OpCodes.Ret);

                il.Emit(OpCodes.Ldsfld, ObserverField);
                il.Emit(OpCodes.Brfalse_S, ret); // Do not call if not initialized
                il.Emit(OpCodes.Ldsfld, ObserverField);
                il.Emit(
                    OpCodes.Callvirt,
                    _hostModule.ImportReference(
                        typeof(IExecutionObserver).GetMethod(nameof(IExecutionObserver.CallCount))
                    )
                );
                il.Append(ret);
            }

            return _callCountMethod;
        }
    }
}

class MethodPatcher
{
    private readonly ExecutionObserverProxyBuilder _proxyBuilder;
    private readonly MethodDefinition _method;
    private bool _alreadyPatched = false;
    private List<Instruction> _allBranchingInstructions;

    internal MethodPatcher(MethodDefinition method, ExecutionObserverProxyBuilder proxyBuilder)
    {
        _method = method;
        _proxyBuilder = proxyBuilder;
    }

    private List<Instruction> AllBranchingInstructions
    {
        get
        {
            if (_allBranchingInstructions == null)
            {
                _allBranchingInstructions = _method.Body.Instructions
                    .Where(i => Constants.JumpingOpCodes.Contains(i.OpCode)).ToList();
            }

            return _allBranchingInstructions;
        }
    }

    public void DoPatch()
    {
        if (!_method.HasBody) return;
        if (_alreadyPatched) return;
        var processor = _method.Body.GetILProcessor();
        processor.Body.SimplifyMacros();
        InsertCallCountAtBeginningOfMethodBody(processor);
        InsertBranchCountForAllBranches(processor);
        processor.Body.OptimizeMacros();
        _alreadyPatched = true;
    }

    private void InsertCallCountAtBeginningOfMethodBody(ILProcessor processor)
    {
        var callCallCountMethod = processor.Create(OpCodes.Call, _proxyBuilder.CallCountMethod);
        processor.InsertBefore(_method.Body.Instructions.First(), callCallCountMethod);
    }

    private void InsertBranchCountForAllBranches(ILProcessor processor)
    {
        static bool IsValidInstruction(Instruction instruction)
        {
            var targetInstruction = (Instruction) instruction.Operand;
            return targetInstruction.Offset < instruction.Offset; // What does this mean?
        }

        foreach (var instruction in AllBranchingInstructions.Where(IsValidInstruction))
        {
            var jumpingDestination = (Instruction) instruction.Operand;
            var callBranchCountMethod = processor.Create(OpCodes.Call, _proxyBuilder.BranchCountMethod);
            processor.InsertAfter(jumpingDestination, callBranchCountMethod);
        }
    }
}