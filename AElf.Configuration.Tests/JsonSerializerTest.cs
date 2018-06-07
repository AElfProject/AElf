using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace AElf.Configuration.Tests
{
    public class JsonSerializerTest
    {
        [Fact]
        public void SerializeTest()
        {
            var model = new JsonModelTest();
            model.ValueString = "sting";
            model.ValueInt = 9;
            model.ValueList = new List<string> {"a", "b", "c"};

            var json = JsonSerializer.Instance.Serialize(model);
            
            Assert.NotNull(json);
        }
        
        [Fact]
        public void DeserializeTest()
        {
            var model = new JsonModelTest();
            model.ValueString = "sting";
            model.ValueInt = 9;
            model.ValueList = new List<string> {"a", "b", "c"};
            
            var json = "{\"ValueString\":\"sting\",\"ValueInt\":9,\"ValueList\":[\"a\",\"b\",\"c\"]}";
            var modelNew = JsonSerializer.Instance.Deserialize<JsonModelTest>(json);
            
            Assert.True(model.Equals(modelNew));
        }
        
        [Fact]
        public void DeserializeTypeTest()
        {
            var model = new JsonModelTest();
            model.ValueString = "sting";
            model.ValueInt = 9;
            model.ValueList = new List<string> {"a", "b", "c"};
            
            var json = "{\"ValueString\":\"sting\",\"ValueInt\":9,\"ValueList\":[\"a\",\"b\",\"c\"]}";
            var modelNew = JsonSerializer.Instance.Deserialize(json,typeof(JsonModelTest));
            
            Assert.True(model.Equals(modelNew));
        }
    }

    public class JsonModelTest
    {
        public string ValueString { get; set; }

        public int ValueInt { get; set; }

        public List<string> ValueList { get; set; }

        public override bool Equals(object obj)
        {
            var model = obj as JsonModelTest;

            if (model == null)
            {
                return false;
            }

            if (model.ValueString != ValueString || model.ValueInt != ValueInt || model.ValueList.Count != ValueList.Count)
            {
                return false;
            }

            for (var i = 0; i < ValueList.Count; i++)
            {
                if (model.ValueList[i] != ValueList[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}