using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CS2TS
{
  internal struct TypeScriptEmitter
  {
    private readonly TextWriter _output;
    private readonly SemanticModel _semanticModel;
    private readonly bool _isDeclaration;

    public TypeScriptEmitter(TextWriter output, SemanticModel semanticModel, bool isDeclaration)
    {
      _output = output;
      _semanticModel = semanticModel;
      _isDeclaration = isDeclaration;
    }

    public void Emit(TypeDeclarationSyntax typeDeclarationSyntax)
    {
      using (var propertyWriter = new StringWriter())
      {
        var emitter = new TypeScriptMemberEmitter(propertyWriter, _semanticModel, _isDeclaration);
        emitter.Visit(typeDeclarationSyntax);
        _output.WriteLine(
          "{1}interface {0}{2} {{",
          typeDeclarationSyntax.Identifier.Text,
          _isDeclaration ? "" : "export ",
          emitter.Extends);
        _output.Write(propertyWriter.ToString());
        _output.WriteLine("}");
        _output.WriteLine();
      }
    }

    public void Emit(EnumDeclarationSyntax enumDeclarationSyntax)
    {
      if (_isDeclaration)
        throw new InvalidOperationException();
      _output.WriteLine("export enum {0} {{", enumDeclarationSyntax.Identifier.Text);
      var currentValue = 0;
      var members = new List<string>();
      foreach (var member in enumDeclarationSyntax.Members)
      {
        var value = currentValue++;
        if (member.EqualsValue != null)
        {
          value = (int) _semanticModel.GetConstantValue(member.EqualsValue.Value).Value;
          currentValue = value + 1;
        }
        members.Add(string.Format("  {0} = {1}", member.Identifier.Text, value));
      }
      _output.WriteLine(string.Join(",\r\n", members));
      _output.WriteLine("}");
      _output.WriteLine();
    }
  }
}