﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using System.Windows.Input;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.Analyzers
{
	[ExportContextMenuEntry(Header = "Analyze", Icon = "images/Search.png", Category = "Analyze", InputGestureText = "Ctrl+R", Order = 100)]
	internal sealed class AnalyzeCommand : SimpleCommand, IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			if (context.TreeView is AnalyzerTreeView && context.SelectedTreeNodes != null && context.SelectedTreeNodes.All(n => n.Parent.IsRoot))
				return false;
			if (context.SelectedTreeNodes == null)
				return context.Reference != null && IsValidReference(context.Reference.Reference);
			return context.SelectedTreeNodes.All(n => n is IMemberTreeNode);
		}

		public bool IsEnabled(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return context.Reference != null && context.Reference.Reference is IEntity;
			foreach (IMemberTreeNode node in context.SelectedTreeNodes) {
				if (!IsValidReference(node.Member))
					return false;
			}

			return true;
		}

		bool IsValidReference(object reference)
		{
			return reference is IEntity;
		}

		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes != null) {
				foreach (IMemberTreeNode node in context.SelectedTreeNodes) {
					AnalyzerTreeView.Instance.Analyze(node.Member);
				}
			} else if (context.Reference != null && context.Reference.Reference is IEntity entity) {
				AnalyzerTreeView.Instance.Analyze(entity);
			}
		}

		public override bool CanExecute(object parameter)
		{
			if (AnalyzerTreeView.Instance.IsKeyboardFocusWithin) {
				return AnalyzerTreeView.Instance.SelectedItems.OfType<object>().All(n => n is IMemberTreeNode);
			} else {
				return MainWindow.Instance.SelectedNodes.All(n => n is IMemberTreeNode);
			}
		}

		public override void Execute(object parameter)
		{
			if (AnalyzerTreeView.Instance.IsKeyboardFocusWithin) {
				foreach (IMemberTreeNode node in AnalyzerTreeView.Instance.SelectedItems.OfType<IMemberTreeNode>().ToArray()) {
					AnalyzerTreeView.Instance.Analyze(node.Member);
				}
			} else {
				foreach (IMemberTreeNode node in MainWindow.Instance.SelectedNodes) {
					AnalyzerTreeView.Instance.Analyze(node.Member);
				}
			}
		}
	}
}
