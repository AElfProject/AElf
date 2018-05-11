using Newtonsoft.Json.Linq;

namespace AElf.Kernel.Node.RPC
{
    internal static class ErrorResponseFactory
    {
        // Version of JSON RPC used
        public const string VERSION = "2.0";
        
        // Simple description strings for the error.
        // Directly taken from the JSON-RPC website
        public static string PARSE_ERR_MSG = "Parse error";
        public static string METH_NOT_FOUND_MSG = "Method not found";

        public static int PARSE_ERR_CODE = -32700;
        public static int METH_NOT_FOUND_CODE = -32601;
        
        /// <summary>
        /// Creates the error object associated with a parse error
        /// </summary>
        /// <param name="id">Request message Id</param>
        /// <returns>The parse error object</returns>
        public static JObject GetParseError(int id)
        {
            return CreateErrorObject(id, PARSE_ERR_CODE, PARSE_ERR_MSG);
        }

        /// <summary>
        /// Creates an error object associated with the method not found error
        /// </summary>
        /// <param name="id">Request message Id</param>
        /// <returns>The parse error object</returns>
        public static JObject GetMethodNotFound(int id)
        {
            return CreateErrorObject(id, METH_NOT_FOUND_CODE, METH_NOT_FOUND_MSG);
        }
        
        private static JObject CreateErrorObject(int id, int code, string msg)
        {
            JObject response = new JObject
            {
                ["jsonrpc"] = VERSION,
                ["id"] = id,
                ["error"] = new JObject
                {
                    ["code"] = code,
                    ["message"] = msg
                }
            };
            
            return response;
        }
    }
}