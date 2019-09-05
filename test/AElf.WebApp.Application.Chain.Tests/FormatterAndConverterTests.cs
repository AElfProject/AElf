using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using AElf.WebApp.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Type = System.Type;

namespace AElf.WebApp.Application.Chain.Tests
{
    public class FormatterAndConverterTests
    {
        private readonly InputFormatter _inputFormatter;
        private readonly OutputFormatter _outputFormatter;
        private readonly JsonConverter _jsonConverter;

        public FormatterAndConverterTests()
        {
            _inputFormatter = new ProtobufInputFormatter();
            _outputFormatter = new ProtobufOutputFormatter();
            _jsonConverter = new ProtoMessageConverter();
        }

        [Fact]
        public async Task InputFormatter_Context_With_Message_Test()
        {
            var hashValue = Hash.FromString("test").ToByteArray();
            var content = hashValue.ToString();
            var context = GenerateInputFormatterContext(content, typeof(Hash), "application/x-protobuf");

            var canRead = _inputFormatter.CanRead(context);
            canRead.ShouldBeTrue();

            await _inputFormatter.ReadRequestBodyAsync(context);
        }

        [Fact]
        public async Task InputFormatter_Context_With_NoneIMessage_Test()
        {
            const string content = "{\"Name\":\"GetBlock\",\"Size\": 100}";
            var context = GenerateInputFormatterContext(content, typeof(TaskQueueInfoDto), null);

            var canRead = _inputFormatter.CanRead(context);
            canRead.ShouldBeFalse();

            var result = await _inputFormatter.ReadRequestBodyAsync(context);
            result.HasError.ShouldBeTrue();
        }

        [Fact]
        public void OutputFormatter_Context_CanWrite_Test()
        {
            var context = GenerateOutputFormatterWriteContext(typeof(Address), null);
            var result = _outputFormatter.CanWriteResult(context);
            result.ShouldBeFalse();

            var address = GenerateAddress();
            context = GenerateOutputFormatterWriteContext(typeof(Address), address, "application/x-protobuf");
            var result2 = _outputFormatter.CanWriteResult(context);
            result2.ShouldBeTrue();
        }

        [Fact]
        public async Task OutputFormatter_WriteResponse_Test()
        {
            var address = GenerateAddress();
            var stream = new MemoryStream();
            var context =
                GenerateOutputFormatterWriteContext(typeof(Address), address, "application/x-protobuf", stream);

            await _outputFormatter.WriteResponseBodyAsync(context);
        }

        [Fact]
        public void JsonConverter_CanConvert_Test()
        {
            var result = _jsonConverter.CanConvert(typeof(TaskQueueInfoDto));
            result.ShouldBeFalse();

            var result1 = _jsonConverter.CanConvert(typeof(Address));
            result1.ShouldBeTrue();
        }

        [Fact]
        public void JsonConverter_ReadJson_Test()
        {
            const string content = "{\"value\":\"EvU6Z9HnVFwt4f7fKlsu5qK4RoEraDxCUJuUM6HO6ic=\"}";
            var serializer = new JsonSerializer();
            var reader = new JsonTextReader(new StringReader(content));

            var result = _jsonConverter.ReadJson(reader, typeof(Address), null, serializer);
            result.ShouldNotBeNull();
            result.GetType().ShouldBe(typeof(Address));
        }

        [Fact]
        public void JsonConverter_WriteJson_Test()
        {
            var address = GenerateAddress();

            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            var writer = new JsonTextWriter(new StringWriter(sb));

            _jsonConverter.WriteJson(writer, address, serializer);
            sb.Length.ShouldBeGreaterThan(0);
            sb.ToString().Contains("value").ShouldBeTrue();
        }

        private InputFormatterContext GenerateInputFormatterContext(string content, Type type,
            string contentType = "application/json")
        {
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(contentBytes);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = contentType;
            httpContext.Request.Body = stream;

            var modelState = new ModelStateDictionary();
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(type);
            var context = new InputFormatterContext(
                httpContext,
                string.Empty,
                modelState,
                metadata,
                new TestHttpRequestStreamReaderFactory().CreateReader);
            return context;
        }

        private OutputFormatterWriteContext GenerateOutputFormatterWriteContext(Type type, object objInfo,
            string contentType = "application/json", MemoryStream stream = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = contentType;
            httpContext.Response.Body = stream ?? new MemoryStream();

            var context = new OutputFormatterWriteContext(
                httpContext,
                Mock.Of<Func<Stream, Encoding, TextWriter>>(),
                type,
                objInfo
            );
            context.ContentType = contentType;

            return context;
        }

        private static Address GenerateAddress()
        {
            var byteArray = new byte[32];
            new Random().NextBytes(byteArray);

            return Address.FromBytes(byteArray);
        }
    }
    
    public class TestHttpRequestStreamReaderFactory : IHttpRequestStreamReaderFactory
    {
        public TextReader CreateReader(Stream stream, Encoding encoding)
        {
            return new HttpRequestStreamReader(stream, encoding);
        }
    }
}