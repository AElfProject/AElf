namespace AElf.Runtime.WebAssembly;

public class OutputBufferTooSmallException : Exception
{
    public OutputBufferTooSmallException() : base()
    {

    }

    public OutputBufferTooSmallException(string message) : base(message)
    {

    }
}