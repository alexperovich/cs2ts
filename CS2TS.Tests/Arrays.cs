using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using Xunit;

namespace CS2TS.Tests
{
  public class Arrays
  {
    public static IEnumerable<object[]> TypeData
    {
      get
      {
        return TypeInformation.TsTypeAndCsTypeParams.PrependParameters(
          new[]
          {
            "IEnumerable<{0}>",
            "ICollection<{0}>",
            "IList<{0}>",
            "IReadOnlyCollection<{0}>",
            "IReadOnlyList<{0}>",
            "{0}[]",
            "List<{0}>",
            "HashSet<{0}>",
            "Collection<{0}>"
          }).AppendParameters(new object[]
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
    [MemberData("TypeData")]
    public void Enumerable(string propertyTypeFormat, string expectedTypescriptType, string memberType, bool generateDeclarations, string typeType)
    {
        var input = string.Format(@"
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Foo
{{
  public {1} Test
  {{
    {2}{0} Prop {{ get; set; }}
  }}
}}
", string.Format(propertyTypeFormat, memberType), typeType, typeType != "interface" ? "public " : "");

        var processor = new TypeScriptProcessor(new []{input}, new[] {typeof(HashSet<>).Assembly.Location});
        var output = processor.GetTypescriptAsString(generateDeclarations);
        Assert.Equal(string.Format(@"{1}interface Test {{
  Prop?: {0}[];
}}

", expectedTypescriptType, generateDeclarations ? "" : "export "), output);
    }
  }
}
