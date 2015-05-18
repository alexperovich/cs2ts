using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CS2TS
{
  internal class TypeCollector : CSharpSyntaxWalker
  {
    public readonly List<TypeDeclarationSyntax> Types = new List<TypeDeclarationSyntax>();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
      Types.Add(node);
      base.VisitClassDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
      Types.Add(node);
      base.VisitStructDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
      Types.Add(node);
      base.VisitInterfaceDeclaration(node);
    }
  }
}