using System;
using System.Collections.Immutable;
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
      Extends = "";
    }

    public string Extends { get; private set; }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
      VisitTypeDeclaration(node);
      base.VisitClassDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
      VisitTypeDeclaration(node);
      base.VisitStructDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
      VisitTypeDeclaration(node);
      base.VisitInterfaceDeclaration(node);
    }

    private void VisitTypeDeclaration(TypeDeclarationSyntax node)
    {
      var typeSymbol = _semanticModel.GetDeclaredSymbol(node);
      ProcessTypeSymbol(typeSymbol);
    }

    private void ProcessTypeSymbol(INamedTypeSymbol typeSymbol)
    {
      ImmutableArray<INamedTypeSymbol> interfaceList;
      if (typeSymbol.BaseType == null || !typeSymbol.BaseType.OriginalDefinition.DeclaringSyntaxReferences.Any())
      {
        interfaceList = typeSymbol.AllInterfaces;
      }
      else
      {
        interfaceList = typeSymbol.Interfaces;
      }
      var dictionary = interfaceList.FirstOrDefault(type => type.MetadataName == "IDictionary`2");
      var enumerable = interfaceList.FirstOrDefault(type => type.MetadataName == "IEnumerable`1");
      if (dictionary != null)
      {
        var keyType = dictionary.TypeArguments[0];
        var valueType = dictionary.TypeArguments[1];
        if (keyType.SpecialType == SpecialType.System_String)
        {
          _output.WriteLine("  [key: string]: {0};", GetTypescriptType(valueType));
        }
        else if (IsNumber(keyType))
        {
          _output.WriteLine("  [key: number]: {0};", GetTypescriptType(valueType));
        }
      }
      else if (enumerable != null)
      {
        var type = enumerable.TypeArguments[0];
        _output.WriteLine("  [index: number]: {0};", GetTypescriptType(type));
      }
      if (typeSymbol.BaseType == null)
        return;
      var baseType = typeSymbol.BaseType.OriginalDefinition;
      if (baseType.DeclaringSyntaxReferences.Any())
      {
        Extends = string.Format(" extends {0}", baseType.Name);
      }
    }

    private static bool IsNumber(ITypeSymbol type)
    {
      switch (type.SpecialType)
      {
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
          return true;
      }
      return false;
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
      if (type.MetadataName == "IDictionary`2")
      {
        var dictionaryType = (INamedTypeSymbol) type;
        var keyType = dictionaryType.TypeArguments[0];
        var valueType = dictionaryType.TypeArguments[1];
        if (keyType.SpecialType == SpecialType.System_String)
        {
          return string.Format("{{ [key: string]: {0}; }}", GetTypescriptType(valueType));
        }
        if (IsNumber(keyType))
        {
          return string.Format("{{ [key: number]: {0}; }}", GetTypescriptType(valueType));
        }
      }
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
      }
      if (_semanticModel.Compilation.Assembly.TypeNames.Any(tn => type.Name == tn))
      {
        return type.Name;
      }
      return type.AllInterfaces.Select(GetTypescriptType).FirstOrDefault(t => t != "any") ?? "any";
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