namespace AElf.Runtime.WebAssembly;

public class OutputBufferTooSmallException : Exception
{
    public OutputBufferTooSmallException()
    {

    }

    public OutputBufferTooSmallException(string message) : base(message)
    {

    }
}