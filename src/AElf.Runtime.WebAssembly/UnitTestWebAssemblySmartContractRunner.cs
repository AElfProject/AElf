namespace AElf.Runtime.WebAssembly;

public class UnitTestWebAssemblySmartContractRunner : WebAssemblySmartContractRunner
{
    public UnitTestWebAssemblySmartContractRunner()
    {
        ExternalEnvironment = new UnitTestExternalEnvironment();
    }
}