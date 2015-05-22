using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CS2TS.Tests
{
  public class EnumTests
  {
    [Fact]
    public void Enum()
    {
      var input = @"
public enum Foo
{
  Bar,
  B,
  Baz = 4,
  Boo = 5,
  Foo = 4,
  A
}";
      var processor = new TypeScriptProcessor(input);
      var output = processor.GetTypescriptAsString(false);
      Assert.Equal(@"export enum Foo {
  Bar = 0,
  B = 1,
  Baz = 4,
  Boo = 5,
  Foo = 4,
  A = 5
}

", output);
    }
  }
}
