namespace AElf.Runtime.WebAssembly.TransactionPayment;

public class WebAssemblyRuntimePaymentException : Exception
{
    public WebAssemblyRuntimePaymentException()
    {

    }

    public WebAssemblyRuntimePaymentException(string message) : base(message)
    {

    }
}