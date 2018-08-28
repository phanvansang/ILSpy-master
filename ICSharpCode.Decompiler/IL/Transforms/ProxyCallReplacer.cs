﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	class ProxyCallReplacer : IILTransform
	{
		public void Run(ILFunction function, ILTransformContext context)
		{
			foreach (var inst in function.Descendants.OfType<CallInstruction>()) {
				Run(inst, context);
			}
		}

		void Run(CallInstruction inst, ILTransformContext context)
		{
			if (inst.Method.MetadataToken.IsNil || inst.Method.MetadataToken.Kind != HandleKind.MethodDefinition)
				return;
			var handle = (MethodDefinitionHandle)inst.Method.MetadataToken;
			if (!IsDefinedInCurrentOrOuterClass(inst.Method, context.Function.Method.DeclaringTypeDefinition))
				return;
			if (!inst.Method.IsCompilerGeneratedOrIsInCompilerGeneratedClass())
				return;
			var metadata = context.PEFile.Metadata;
			MethodDefinition methodDef = metadata.GetMethodDefinition((MethodDefinitionHandle)inst.Method.MetadataToken);
			if (!methodDef.HasBody())
				return;
			var genericContext = DelegateConstruction.GenericContextFromTypeArguments(inst.Method.Substitution);
			if (genericContext == null)
				return;
			// partially copied from CSharpDecompiler
			var ilReader = context.CreateILReader();
			var body = context.PEFile.Reader.GetMethodBody(methodDef.RelativeVirtualAddress);
			var proxyFunction = ilReader.ReadIL(handle, body, genericContext.Value, context.CancellationToken);
			var transformContext = new ILTransformContext(context, proxyFunction);
			proxyFunction.RunTransforms(CSharp.CSharpDecompiler.EarlyILTransforms(), transformContext);
			if (!(proxyFunction.Body is BlockContainer blockContainer))
				return;
			if (blockContainer.Blocks.Count != 1)
				return;
			var block = blockContainer.Blocks[0];
			Call call = null;
			if (block.Instructions.Count == 1) {
				// leave IL_0000 (call Test(ldloc this, ldloc A_1))
				if (!block.Instructions[0].MatchLeave(blockContainer, out ILInstruction returnValue))
					return;
				call = returnValue as Call;
			} else if (block.Instructions.Count == 2) {
				// call Test(ldloc this, ldloc A_1)
				// leave IL_0000(nop)
				call = block.Instructions[0] as Call;
				if (!block.Instructions[1].MatchLeave(blockContainer, out ILInstruction returnValue))
					return;
				if (!returnValue.MatchNop())
					return;
			}
			if (call == null) {
				return;
			}
			if (call.Method.IsConstructor)
				return;

			// check if original arguments are only correct ldloc calls
			for (int i = 0; i < call.Arguments.Count; i++) {
				var originalArg = call.Arguments[i];
				if (!originalArg.MatchLdLoc(out ILVariable var) ||
					var.Kind != VariableKind.Parameter ||
					var.Index != i - 1) {
					return;
				}
			}

			Call newInst = (Call)call.Clone();

			newInst.Arguments.ReplaceList(inst.Arguments);
			inst.ReplaceWith(newInst);
		}

		static bool IsDefinedInCurrentOrOuterClass(IMethod method, ITypeDefinition declaringTypeDefinition)
		{
			while (declaringTypeDefinition != null) {
				if (method.DeclaringTypeDefinition == declaringTypeDefinition)
					return true;
				declaringTypeDefinition = declaringTypeDefinition.DeclaringTypeDefinition;
			}
			return false;
		}
	}
}
