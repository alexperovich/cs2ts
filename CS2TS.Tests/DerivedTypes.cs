using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CS2TS.Tests
{
  public class DerivedTypes
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
          });
      }
    }

    [Theory]
    [MemberData("Parameters")]
    public void Dictionary(string typescriptType, string cSharpType, bool generateDeclarations)
    {
      var input = string.Format(@"
using System.Collections.Generic;

namespace Foo
{{
  public class Test : Dictionary<int, {0}>
  {{
  }}
  public class Test2 : Dictionary<string, {0}>
  {{
  }}
  public class Test3 : Test2
  {{
    public {0} Prop {{ get; set; }}
  }}
}}
", cSharpType);

      var processor = new TypeScriptProcessor(input);
      var output = processor.GetTypescriptAsString(generateDeclarations);
      Assert.Equal(string.Format(@"{2} interface Test {{
  [key: number]: {0};
}}

{2} interface Test2 {{
  [key: string]: {0};
}}

{2} interface Test3 extends Test2 {{
  Prop{1}: {0};
}}

", typescriptType, cSharpType.ToLower().Contains("string") ? "?" : "", generateDeclarations ? "declare" : "export"), output);
    }
  }
}
