﻿using System.Collections.Generic;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Tests.Helpers
{
	class RemoveCompilerAttribute : DepthFirstAstVisitor, IAstTransform
	{
		public override void VisitAttribute(CSharp.Syntax.Attribute attribute)
		{
			var section = (AttributeSection)attribute.Parent;
			SimpleType type = attribute.Type as SimpleType;
			if (section.AttributeTarget == "assembly" &&
				(type.Identifier == "CompilationRelaxations" || type.Identifier == "RuntimeCompatibility" || type.Identifier == "SecurityPermission" || type.Identifier == "PermissionSet" || type.Identifier == "AssemblyVersion" || type.Identifier == "Debuggable"))
			{
				attribute.Remove();
				if (section.Attributes.Count == 0)
					section.Remove();
			}
			if (section.AttributeTarget == "module" && type.Identifier == "UnverifiableCode")
			{
				attribute.Remove();
				if (section.Attributes.Count == 0)
					section.Remove();
			}
		}

		public void Run(AstNode rootNode, TransformContext context)
		{
			rootNode.AcceptVisitor(this);
		}
	}

	public class RemoveEmbeddedAtttributes : DepthFirstAstVisitor, IAstTransform
	{
		HashSet<string> attributeNames = new HashSet<string>() {
			"System.Runtime.CompilerServices.IsReadOnlyAttribute",
			"System.Runtime.CompilerServices.IsByRefLikeAttribute",
			"Microsoft.CodeAnalysis.EmbeddedAttribute",
		};

		public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
		{
			var typeDefinition = typeDeclaration.GetSymbol() as ITypeDefinition;
			if (typeDefinition == null || !attributeNames.Contains(typeDefinition.FullName))
				return;
			if (typeDeclaration.Parent is NamespaceDeclaration ns && ns.Members.Count == 1)
				ns.Remove();
			else
				typeDeclaration.Remove();
		}

		public void Run(AstNode rootNode, TransformContext context)
		{
			rootNode.AcceptVisitor(this);
		}
	}
}
