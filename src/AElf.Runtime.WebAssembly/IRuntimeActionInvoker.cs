using AElf.Runtime.WebAssembly.Contract;
using Volo.Abp.DependencyInjection;
using Wasmtime;

namespace AElf.Runtime.WebAssembly;

public interface IRuntimeActionInvoker : ITransientDependency
{
    InvokeResult Invoke(Func<ActionResult> action);
}

public class RuntimeActionInvoker : IRuntimeActionInvoker
{
    public InvokeResult Invoke(Func<ActionResult>? action)
    {
        try
        {
            var result = action?.Invoke();
            if (result == null)
            {
                throw new WebAssemblyRuntimeException("Failed to invoke action.");
            }

            if (result.Value.Trap.Message.Contains("wasm `unreachable` instruction executed") &&
                result.Value.Trap.Frames?.Count <= 2)
            {
                // Ignore.
            }
            else
            {
                throw result.Value.Trap;
            }
        }
        catch (Exception ex)
        {
            return new InvokeResult
            {
                Success = false,
                DebugMessage = ex.ToString()
            };
        }

        return new InvokeResult
        {
            Success = true
        };
    }
}

public class InvokeResult
{
    public bool Success { get; set; }
    public string DebugMessage { get; set; }
}