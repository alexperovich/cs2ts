using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CS2TS
{
  internal class TypeScriptMemberEmitter : CSharpSyntaxWalker
  {
    private readonly TextWriter _output;
    private readonly SemanticModel _semanticModel;
    private readonly bool _isDeclaration;

    public TypeScriptMemberEmitter(TextWriter output, SemanticModel semanticModel, bool isDeclaration)
    {
      _output = output;
      _semanticModel = semanticModel;
      _isDeclaration = isDeclaration;
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
      var propertySymbol = _semanticModel.GetDeclaredSymbol(node);
      _output.WriteLine("  {0}{2}: {1};", GetPropertyName(propertySymbol), GetTypescriptType(propertySymbol.Type), IsNullable(propertySymbol.Type) ? "?" : "");
      base.VisitPropertyDeclaration(node);
    }

    private string GetPropertyName(IPropertySymbol propertySymbol)
    {
      var name = propertySymbol.Name;
      var attributes = propertySymbol.GetAttributes();
      var jsonPropertyAttribute =
        attributes
          .FirstOrDefault(a => a.AttributeClass.Name == "JsonPropertyAttribute");
      if (jsonPropertyAttribute != null)
      {
        if (jsonPropertyAttribute.ConstructorArguments.Length > 0)
        {
          var firstArgument = jsonPropertyAttribute.ConstructorArguments.First();
          if (firstArgument.Type.SpecialType == SpecialType.System_String)
          {
            name = (string)firstArgument.Value;
          }
        }
      }
      var dataMemberAttribute = attributes.FirstOrDefault(a => a.AttributeClass.Name == "DataMemberAttribute");
      if (dataMemberAttribute != null)
      {
        var namedArguments = dataMemberAttribute.NamedArguments;
        if (namedArguments.Length > 0 && namedArguments.Any(p => p.Key == "Name"))
        {
          var nameArgument = namedArguments.First(p => p.Key == "Name");
          if (nameArgument.Value.Type.SpecialType == SpecialType.System_String)
          {
            name = (string) nameArgument.Value.Value;
          }
        }
      }
      return name;
    }

    private string GetTypescriptType(ITypeSymbol type)
    {
      if (type.TypeKind == TypeKind.Array)
      {
        IArrayTypeSymbol arr = (IArrayTypeSymbol) type;
        return GetTypescriptType(arr.ElementType) + "[]";
      }
      switch (type.SpecialType)
      {
        case SpecialType.System_Object:
          return "any";
        case SpecialType.System_Boolean:
          return "boolean";
        case SpecialType.System_Char:
        case SpecialType.System_SByte:
        case SpecialType.System_Byte:
        case SpecialType.System_Int16:
        case SpecialType.System_UInt16:
        case SpecialType.System_Int32:
        case SpecialType.System_UInt32:
        case SpecialType.System_Int64:
        case SpecialType.System_UInt64:
        case SpecialType.System_Decimal:
        case SpecialType.System_Single:
        case SpecialType.System_Double:
          return "number";
        case SpecialType.System_String:
          return "string";
        case SpecialType.System_Collections_IEnumerable:
          return "any[]";
      }
      switch (type.OriginalDefinition.SpecialType)
      {
        case SpecialType.System_Collections_Generic_IEnumerable_T:
        case SpecialType.System_Collections_Generic_ICollection_T:
        case SpecialType.System_Collections_Generic_IReadOnlyCollection_T:
        case SpecialType.System_Collections_Generic_IList_T:
        case SpecialType.System_Collections_Generic_IReadOnlyList_T:
          var namedType = (INamedTypeSymbol) type;
          return GetTypescriptType(namedType.TypeArguments.First()) + "[]";
        default:
          if (_semanticModel.Compilation.Assembly.TypeNames.Any(tn => type.Name == tn))
          {
            return type.Name;
          }
          return type.AllInterfaces.Select(GetTypescriptType).FirstOrDefault(t => t != "any") ?? "any";
      }
    }

    private static bool IsNullable(ITypeSymbol type)
    {
      return type.IsReferenceType;
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
      var declaration = node.Declaration;
      foreach (var variable in declaration.Variables)
      {
        var field = (IFieldSymbol) _semanticModel.GetDeclaredSymbol(variable);
        var type = field.Type;
        _output.WriteLine("  {0}{2}: {1};", variable.Identifier.Text, GetTypescriptType(type), IsNullable(type) ? "?" : "");
      }
      base.VisitFieldDeclaration(node);
    }
  }
}