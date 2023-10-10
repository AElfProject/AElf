using System;
using System.Runtime.InteropServices;

namespace Solang
{
    public unsafe delegate int build_wasm(void* src, void* outputBuffer, int outputMaxLength);
    
    /// <summary>
    /// Type for error and illegal callback functions,
    /// </summary>
    /// <param name="message">message: error message.</param>
    /// <param name="data">data: callback marker, it is set by user together with callback.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void ErrorCallbackDelegate(string message, void* data);
}