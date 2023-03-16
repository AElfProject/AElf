namespace AElf.CSharp.CodeOps;

internal class SourceCodeBuilder
{
    private readonly string _namespace;
    private readonly string _contractTypeName = "Contract";
    private readonly string _stateTypeName = "StateType";
    private readonly List<string> _otherClasses = new();
    private readonly List<string> _classesNestedInContract = new();
    private readonly List<string> _stateFields = new();
    private readonly List<string> _methods = new();

    internal SourceCodeBuilder(string namespace_ = "__Contract__")
    {
        _namespace = namespace_;
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

    internal string ContractTypeFullName => $"{_namespace}.{_contractTypeName}";
    internal string StateTypeName => _stateTypeName;

    private string NestedClassesCode => _classesNestedInContract.JoinAsString("\n");
    private string OtherClassesCode => _otherClasses.JoinAsString("\n");
    private string StateFieldsCode => _stateFields.JoinAsString("\n");
    private string MethodsCode => _methods.JoinAsString("\n");

    internal string Build()
    {
        return @"
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace " + _namespace + @"
{
    public class " + _stateTypeName + @" : ContractState
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

    public class " + _contractTypeName + @" : Container.ContractBase
    {
" + MethodsCode + @"
" + NestedClassesCode + @"
    }
}
";
    }
}