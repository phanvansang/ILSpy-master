﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System.Linq;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using Microsoft.Win32;

namespace TestPlugin
{
	[ExportContextMenuEntryAttribute(Header = "_Save Assembly")]
	public class SaveAssembly : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.SelectedTreeNodes != null && context.SelectedTreeNodes.All(n => n is AssemblyTreeNode);
		}
		
		public bool IsEnabled(TextViewContext context)
		{
			return context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length == 1;
		}
		
		public void Execute(TextViewContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			AssemblyTreeNode node = (AssemblyTreeNode)context.SelectedTreeNodes[0];
			var asm = node.LoadedAssembly.GetPEFileOrNull();
			if (asm != null) {
				/*SaveFileDialog dlg = new SaveFileDialog();
				dlg.FileName = node.LoadedAssembly.FileName;
				dlg.Filter = "Assembly|*.dll;*.exe";
				if (dlg.ShowDialog(MainWindow.Instance) == true) {
					asm.MainModule.Write(dlg.FileName);
				}*/
			}
		}
	}
}
