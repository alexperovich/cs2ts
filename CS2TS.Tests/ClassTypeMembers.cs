using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CS2TS.Tests
{
  public class ClassTypeMembers
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
    public void Property(
      string typescriptType,
      string cSharpType,
      bool generateDeclarations,
      string typeType)
    {
      var input = string.Format(@"
using System;

namespace Foo
{{
  public {1} Test
  {{
    {2}{0} Prop {{ get; set; }}
  }}
  public {1} Test2
  {{
    {2}Test Prop {{ get; set; }}
  }}
}}
", cSharpType, typeType, typeType != "interface" ? "public " : "");

      var processor = new TypeScriptProcessor(input);
      var output = processor.GetTypescriptAsString(generateDeclarations);
      Assert.Equal(string.Format(@"{2}interface Test {{
  Prop{1}: {0};
}}

{2}interface Test2 {{
  Prop{3}: Test;
}}

", typescriptType, cSharpType.ToLower().Contains("string") ? "?" : "", generateDeclarations ? "" : "export ", typeType == "struct" ? "" : "?"), output);
    }
  }
}
