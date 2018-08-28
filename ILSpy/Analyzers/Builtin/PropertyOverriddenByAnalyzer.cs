﻿// Copyright (c) 2018 Siegfried Pammer
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.ILSpy.Analyzers.Builtin
{
	/// <summary>
	/// Shows properties that override a property.
	/// </summary>
	[ExportAnalyzer(Header = "Overridden By", Order = 20)]
	class PropertyOverriddenByAnalyzer : IAnalyzer
	{
		public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
		{
			Debug.Assert(analyzedSymbol is IProperty);
			var scope = context.GetScopeOf((IProperty)analyzedSymbol);
			foreach (var type in scope.GetTypesInScope(context.CancellationToken)) {
				foreach (var result in AnalyzeType((IProperty)analyzedSymbol, type))
					yield return result;
			}
		}

		IEnumerable<IEntity> AnalyzeType(IProperty analyzedEntity, ITypeDefinition type)
		{
			if (!analyzedEntity.DeclaringType.GetAllBaseTypeDefinitions()
				.Any(t => t.MetadataToken == analyzedEntity.DeclaringTypeDefinition.MetadataToken && t.ParentModule.PEFile == type.ParentModule.PEFile))
				yield break;

			foreach (var property in type.Properties) {
				if (!property.IsOverride) continue;
				if (InheritanceHelper.GetBaseMembers(property, false)
					.Any(p => p.MetadataToken == analyzedEntity.MetadataToken &&
							  p.ParentModule.PEFile == analyzedEntity.ParentModule.PEFile)) {
					yield return property;
				}
			}
		}

		public bool Show(ISymbol entity)
		{
			return entity is IProperty property && property.IsOverridable && property.DeclaringType.Kind != TypeKind.Interface;
		}
	}
}
