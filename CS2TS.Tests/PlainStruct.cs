using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace CS2TS.Tests
{
  public class PlainType
  {
    public static IEnumerable<object[]> Parameters
    {
      get
      {
        return TypeInformation.TsTypeAndCsTypeParams.AppendParameters(
          new object[]
          {
            true,
            false
          }).AppendParameters(new object[]
          {
            "struct",
            "class",
            "interface"
          });
      }
    }

    [Theory]
    [MemberData("Parameters")]
    public void SingleNumberPropertyStruct(string typescriptType, string cSharpType, bool generateDeclarations, string typeType)
    {
      var input = string.Format(@"
using System;

namespace Foo
{{
  public {1} Test
  {{
    {2}{0} Prop {{ get; set; }}
  }}
}}
", cSharpType, typeType, typeType != "interface" ? "public " : "");

      var processor = new TypeScriptProcessor(input);
      var output = processor.GetTypescriptAsString(generateDeclarations);
      Assert.Equal(string.Format(@"{2} interface Test {{
  Prop{1}: {0};
}}

", typescriptType, cSharpType.ToLower().Contains("string") ? "?" : "", generateDeclarations ? "declare" : "export"), output);
    }

    [Theory]
    [MemberData("Parameters")]
    public void MultipleFieldStruct(string typescriptType, string cSharpType, bool generateDeclarations, string typeType)
    {
      var input = string.Format(@"
using System;

namespace Foo
{{
  public {1} Test
  {{
    {2}{0} x, y;
  }}
}}
", cSharpType, typeType, typeType != "interface" ? "public " : "");

      var processor = new TypeScriptProcessor(input);
      var output = processor.GetTypescriptAsString(generateDeclarations);
      Assert.Equal(string.Format(@"{2} interface Test {{
  x{1}: {0};
  y{1}: {0};
}}

", typescriptType, cSharpType.ToLower().Contains("string") ? "?" : "", generateDeclarations ? "declare" : "export"), output);
    }

    [Theory]
    [MemberData("Parameters")]
    public void JsonProperty(string typescriptType, string cSharpType, bool generateDeclarations, string typeType)
    {
      var input = string.Format(@"
using System;
using Newtonsoft.Json;

namespace Foo
{{
  public {1} Test
  {{
    [JsonProperty(""foo"")]
    {2}{0} Prop {{ get; set; }}
  }}
}}
", cSharpType, typeType, typeType != "interface" ? "public " : "");

      var processor = new TypeScriptProcessor(new[] {input}, new[] {typeof (JsonPropertyAttribute).Assembly.Location});
      var output = processor.GetTypescriptAsString(generateDeclarations);
      Assert.Equal(string.Format(@"{2} interface Test {{
  foo{1}: {0};
}}

", typescriptType, cSharpType.ToLower().Contains("string") ? "?" : "", generateDeclarations ? "declare" : "export"), output);
    }

    [Theory]
    [MemberData("Parameters")]
    public void DataMember(string typescriptType, string cSharpType, bool generateDeclarations, string typeType)
    {
      var input = string.Format(@"
using System;
using System.Runtime.Serialization;

namespace Foo
{{
  [DataContract]
  public {1} Test
  {{
    [DataMember(Name = ""foo"")]
    {2}{0} Prop {{ get; set; }}
  }}
}}
", cSharpType, typeType, typeType != "interface" ? "public " : "");

      var processor = new TypeScriptProcessor(new[] {input}, new[] {typeof (DataMemberAttribute).Assembly.Location});
      var output = processor.GetTypescriptAsString(generateDeclarations);
      Assert.Equal(string.Format(@"{2} interface Test {{
  foo{1}: {0};
}}

", typescriptType, cSharpType.ToLower().Contains("string") ? "?" : "", generateDeclarations ? "declare" : "export"), output);
    }
  }
}
