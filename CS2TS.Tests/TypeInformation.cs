using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2TS.Tests
{
  public static class TypeInformation
  {
    public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T value)
    {
      foreach (var t in source)
        yield return t;
      yield return value;
    }

    public static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, T value)
    {
      yield return value;
      foreach (var t in source)
        yield return t;
    }

    public static IEnumerable<object[]> PrependParameters(
      this IEnumerable<object[]> sourceParameters,
      IEnumerable<object> newParameters)
    {
      IEnumerable<object[]> parameters = sourceParameters as object[][] ?? sourceParameters.ToArray();
      return from newParam in newParameters
        from memberTypes in parameters
        select memberTypes.Prepend(newParam).ToArray();
    }

    public static IEnumerable<object[]> AppendParameters(
      this IEnumerable<object[]> sourceParameters,
      IEnumerable<object> newParameters)
    {
      IEnumerable<object[]> parameters = sourceParameters as object[][] ?? sourceParameters.ToArray();
      return from newParam in newParameters
        from memberTypes in parameters
        select memberTypes.Append(newParam).ToArray();
    }

    private static readonly ILookup<string, string> _cSharpTypesForTypescriptType = new[]
    {
      new[]{"number", "byte"}, 
      new[]{"number", "sbyte"}, 
      new[]{"number", "long"}, 
      new[]{"number", "ulong"}, 
      new[]{"number", "short"}, 
      new[]{"number", "ushort"}, 
      new[]{"number", "int"}, 
      new[]{"number", "uint"}, 
      new[]{"number", "float"}, 
      new[]{"number", "double"}, 
      new[]{"number", "decimal"},
      new[]{"string", "string"},
      new[]{"boolean", "bool"},
    }.ToLookup(arr => arr[0], arr => arr[1]);


    public static ILookup<string, string> CSharpTypesForTypescriptType
    {
      get { return _cSharpTypesForTypescriptType; }
    }

    public static IEnumerable<object[]> TsTypeAndCsTypeParams
    {
      get
      {
        return
          CSharpTypesForTypescriptType.SelectMany(
            s => s.Select(csType => new object[] {s.Key, csType}));
      }
    }
  }
}
