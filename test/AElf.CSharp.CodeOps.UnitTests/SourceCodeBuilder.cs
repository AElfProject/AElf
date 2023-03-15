namespace AElf.CSharp.CodeOps;

internal class SourceCodeBuilder
{
    private readonly string _namespace;
    private readonly string _contractTypeName = "Contract";
    private readonly List<string> _otherClasses = new();
    private readonly List<string> _classesNestedInContract = new();

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

    internal string ContractTypeFullName => $"{_namespace}.{_contractTypeName}";

    private string NestedClassesCode => _classesNestedInContract.JoinAsString("\n");
    private string OtherClassesCode => _otherClasses.JoinAsString("\n");

    internal string Build()
    {
        return @"
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace " + _namespace + @"
{
    public class State : ContractState
    {
    }

    public class Container
    {
        public class ContractBase : AElf.Sdk.CSharp.CSharpSmartContract<State>
        {
        }
    }

" + OtherClassesCode + @"

    public class " + _contractTypeName + @" : Container.ContractBase
    {
" + NestedClassesCode + @"
    }
}
";
    }
}