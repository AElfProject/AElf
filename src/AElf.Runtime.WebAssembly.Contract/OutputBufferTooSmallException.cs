namespace AElf.Runtime.WebAssembly.Contract;

public class OutputBufferTooSmallException : Exception
{
    public OutputBufferTooSmallException()
    {

    }

    public OutputBufferTooSmallException(string message) : base(message)
    {

    }
}