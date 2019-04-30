using System.Diagnostics;
using ChakraCore.NET;
using ChakraCore.NET.API;

namespace AElf.CLI.JS
{
    public static class JSEngineExtension
    {
        public static string[] GetObjectPropertyNames(this IJSEngine engine, JavaScriptValue obj)
        {
            return engine.GlobalObject.CallFunction<JavaScriptValue, string>("_getOwnPropertyNames", obj)
                .Split(",");
        }

        public static JavaScriptValue GetObjectProperty(this IJSEngine engine, JavaScriptValue obj, string propertyName)
        {
            return engine.GlobalObject
                .CallFunction<JavaScriptValue, string, JavaScriptValue>("_getOwnProperty", obj, propertyName);
        }

        public static string GetFunctionToString(this IJSEngine engine, JavaScriptValue func)
        {
            Debug.Assert(func.ValueType == JavaScriptValueType.Function);
            return new JSValue(engine.ServiceNode, func).CallFunction<string>("toString");
        }

        public static int GetArraySize(this IJSEngine engine, JavaScriptValue array)
        {
            Debug.Assert(array.ValueType == JavaScriptValueType.Array);
            return engine.GlobalObject.CallFunction<JavaScriptValue, string, int>("_getOwnProperty", array, "length");
        }

        public static void AssignToUnderscore(this IJSEngine engine, JavaScriptValue self)
        {
            engine.GlobalObject.CallMethod("_assignToUnderscore", self);
        }
    }
}