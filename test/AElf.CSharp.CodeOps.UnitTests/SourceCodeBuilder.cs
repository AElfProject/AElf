namespace AElf.CSharp.CodeOps;

internal class SourceCodeBuilder
{
    protected readonly string Namespace;
    protected const string ContractTypeName = "Contract";
    private const string _stateTypeName = "StateType";
    private readonly List<string> _otherClasses = new();
    private readonly List<string> _classesNestedInContract = new();
    private readonly List<string> _stateFields = new();
    private readonly List<string> _methods = new();

    internal SourceCodeBuilder(string @namespace = "__Contract__")
    {
        Namespace = @namespace;
    }

    internal SourceCodeBuilder AddClass(string source, bool isNestedInContract = false)
    {
        if (isNestedInContract)
        {
            _classesNestedInContract.Add(source);
        }
        else
        {
            _otherClasses.Add(source);
        }

        return this;
    }

    internal SourceCodeBuilder AddStateField(string source)
    {
        _stateFields.Add(source);
        return this;
    }

    internal SourceCodeBuilder AddMethod(string source)
    {
        _methods.Add(source);
        return this;
    }

    internal string ContractTypeFullName => $"{Namespace}.{ContractTypeName}";
    internal string StateTypeName => _stateTypeName;

    protected string NestedClassesCode => _classesNestedInContract.JoinAsString("\n");
    protected string OtherClassesCode => _otherClasses.JoinAsString("\n");
    protected string StateFieldsCode => _stateFields.JoinAsString("\n");
    protected string MethodsCode => _methods.JoinAsString("\n");

    public virtual string Build()
    {
        return @"
using System;
using System.Runtime.InteropServices;
using AElf.Kernel.SmartContract;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace " + Namespace + @"
{
    public class " + StateTypeName + @" : ContractState
    {

" + StateFieldsCode + @"

    }

    public class Container
    {
        public class ContractBase : AElf.Sdk.CSharp.CSharpSmartContract<" + _stateTypeName + @">
        {
        }
    }

" + OtherClassesCode + @"

    public class " + ContractTypeName + @" : Container.ContractBase
    {
" + MethodsCode + @"
" + NestedClassesCode + @"
    }
}
";
    }
}