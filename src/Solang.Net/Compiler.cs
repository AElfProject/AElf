using System;
using System.Runtime.InteropServices;
using System.Text;
using Google.Protobuf;

namespace Solang
{
    public unsafe class Compiler
    {
        private static byte[] Buffer = new byte[2097152];
        private static object _ = new object();

        static readonly Lazy<build_wasm> build_wasm
            = LazyDelegate<build_wasm>(nameof(build_wasm));

        internal const string Lib = "solang_wrapper";

        public static string LibPath => _libPath.Value;
        private static readonly Lazy<string> _libPath = new Lazy<string>(() => LibPathResolver.Resolve(Lib));
        private static readonly Lazy<IntPtr> LibPtr = new Lazy<IntPtr>(() => LoadLibNative.LoadLib(_libPath.Value));

        static Lazy<TDelegate> LazyDelegate<TDelegate>(string symbol)
        {
            return new Lazy<TDelegate>(() => { return LoadLibNative.GetDelegate<TDelegate>(LibPtr.Value, symbol); },
                isThreadSafe: false);
        }

        public Output BuildWasm(byte[] source)
        {
            lock (_)
            {
                Span<byte> output = Buffer;
                var returnedBytes = 0;

                fixed (byte* srcPtr = &MemoryMarshal.GetReference((Span<byte>)source),
                       buffPtr = &MemoryMarshal.GetReference(output))
                {
                    returnedBytes = build_wasm.Value(srcPtr, buffPtr, Buffer.Length);
                }

                if (returnedBytes <= 0) throw new Exception("failed");

                var bytes = new ArraySegment<byte>(Buffer, 0, returnedBytes);
                var bs = ByteString.CopyFrom(bytes);
                return Output.Parser.ParseFrom(bs);
            }
        }
    }
}