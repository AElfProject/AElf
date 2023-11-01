namespace AElf.Runtime.WebAssembly.Contract;

public class WebAssemblyRuntimeException : Exception
{
    public WebAssemblyRuntimeException()
    {

    }

    public WebAssemblyRuntimeException(string message) : base(message)
    {

    }
}