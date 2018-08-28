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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Threading;

using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.Options;

using Microsoft.VisualStudio.Composition;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		
		internal static CommandLineArguments CommandLineArguments;

		static ExportProvider exportProvider;
		
		public static ExportProvider ExportProvider => exportProvider;

		static IExportProviderFactory exportProviderFactory;
		
		public static IExportProviderFactory ExportProviderFactory => exportProviderFactory;
		
		internal static readonly IList<ExceptionData> StartupExceptions = new List<ExceptionData>();
		
		internal class ExceptionData
		{
			public Exception Exception;
			public string PluginName;
		}
		
		public App()
		{
			var cmdArgs = Environment.GetCommandLineArgs().Skip(1);
			App.CommandLineArguments = new CommandLineArguments(cmdArgs);
			if ((App.CommandLineArguments.SingleInstance ?? true) && !MiscSettingsPanel.CurrentMiscSettings.AllowMultipleInstances) {
				cmdArgs = cmdArgs.Select(FullyQualifyPath);
				string message = string.Join(Environment.NewLine, cmdArgs);
				if (SendToPreviousInstance("ILSpy:\r\n" + message, !App.CommandLineArguments.NoActivate)) {
					Environment.Exit(0);
				}
			}
			InitializeComponent();
			
			// Cannot show MessageBox here, because WPF would crash with a XamlParseException
			// Remember and show exceptions in text output, once MainWindow is properly initialized
			try {
				// Set up VS MEF. For now, only do MEF1 part discovery, since that was in use before.
				// To support both MEF1 and MEF2 parts, just change this to:
				// var discovery = PartDiscovery.Combine(new AttributedPartDiscoveryV1(Resolver.DefaultInstance),
				//                                       new AttributedPartDiscovery(Resolver.DefaultInstance));
				var discovery = new AttributedPartDiscoveryV1(Resolver.DefaultInstance);
				var catalog = ComposableCatalog.Create(Resolver.DefaultInstance);
				var pluginDir = Path.GetDirectoryName(typeof(App).Module.FullyQualifiedName);
				if (pluginDir != null) {
					foreach (var plugin in Directory.GetFiles(pluginDir, "*.Plugin.dll")) {
						var name = Path.GetFileNameWithoutExtension(plugin);
						try {
							var asm = Assembly.Load(name);
							var parts = discovery.CreatePartsAsync(asm).Result;
							catalog = catalog.AddParts(parts);
						} catch (Exception ex) {
							StartupExceptions.Add(new ExceptionData { Exception = ex, PluginName = name });
						}
					}
				}
				// Add the built-in parts
				catalog = catalog.AddParts(discovery.CreatePartsAsync(Assembly.GetExecutingAssembly()).Result);
				// If/When the project switches to .NET Standard/Core, this will be needed to allow metadata interfaces (as opposed
				// to metadata classes). When running on .NET Framework, it's automatic.
				//   catalog.WithDesktopSupport();
				// If/When any part needs to import ICompositionService, this will be needed:
				//   catalog.WithCompositionService();
				var config = CompositionConfiguration.Create(catalog);
				exportProviderFactory = config.CreateExportProviderFactory();
				exportProvider = exportProviderFactory.CreateExportProvider();
				// This throws exceptions for composition failures. Alternatively, the configuration's CompositionErrors property
				// could be used to log the errors directly. Used at the end so that it does not prevent the export provider setup.
				config.ThrowOnErrors();
			}
			catch (Exception ex) {
				StartupExceptions.Add(new ExceptionData { Exception = ex });
			}
			
			if (!System.Diagnostics.Debugger.IsAttached) {
				AppDomain.CurrentDomain.UnhandledException += ShowErrorBox;
				Dispatcher.CurrentDispatcher.UnhandledException += Dispatcher_UnhandledException;
			}
			TaskScheduler.UnobservedTaskException += DotNet40_UnobservedTaskException;
			Languages.Initialize(exportProvider);

			EventManager.RegisterClassHandler(typeof(Window),
			                                  Hyperlink.RequestNavigateEvent,
			                                  new RequestNavigateEventHandler(Window_RequestNavigate));
			ILSpyTraceListener.Install();
		}
		
		string FullyQualifyPath(string argument)
		{
			// Fully qualify the paths before passing them to another process,
			// because that process might use a different current directory.
			if (string.IsNullOrEmpty(argument) || argument[0] == '/')
				return argument;
			try {
				return Path.Combine(Environment.CurrentDirectory, argument);
			} catch (ArgumentException) {
				return argument;
			}
		}

		void DotNet40_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			// On .NET 4.0, an unobserved exception in a task terminates the process unless we mark it as observed
			e.SetObserved();
		}
		
		#region Exception Handling
		static void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			UnhandledException(e.Exception);
			e.Handled = true;
		}
		
		static void ShowErrorBox(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = e.ExceptionObject as Exception;
			if (ex != null) {
				UnhandledException(ex);
			}
		}

		static void UnhandledException(Exception exception)
		{
			Debug.WriteLine(exception.ToString());
			for (Exception ex = exception; ex != null; ex = ex.InnerException) {
				ReflectionTypeLoadException rtle = ex as ReflectionTypeLoadException;
				if (rtle != null && rtle.LoaderExceptions.Length > 0) {
					exception = rtle.LoaderExceptions[0];
					Debug.WriteLine(exception.ToString());
					break;
				}
			}
			MessageBox.Show(exception.ToString(), "Sorry, we crashed");
		}
		#endregion

		#region Pass Command Line Arguments to previous instance
		bool SendToPreviousInstance(string message, bool activate)
		{
			bool success = false;
			NativeMethods.EnumWindows(
				(hWnd, lParam) => {
					string windowTitle = NativeMethods.GetWindowText(hWnd, 100);
					if (windowTitle.StartsWith("ILSpy", StringComparison.Ordinal)) {
						Debug.WriteLine("Found {0:x4}: {1}", hWnd, windowTitle);
						IntPtr result = Send(hWnd, message);
						Debug.WriteLine("WM_COPYDATA result: {0:x8}", result);
						if (result == (IntPtr)1) {
							if (activate)
								NativeMethods.SetForegroundWindow(hWnd);
							success = true;
							return false; // stop enumeration
						}
					}
					return true; // continue enumeration
				}, IntPtr.Zero);
			return success;
		}
		
		unsafe static IntPtr Send(IntPtr hWnd, string message)
		{
			const uint SMTO_NORMAL = 0;
			
			CopyDataStruct lParam;
			lParam.Padding = IntPtr.Zero;
			lParam.Size = message.Length * 2;
			fixed (char *buffer = message) {
				lParam.Buffer = (IntPtr)buffer;
				IntPtr result;
				// SendMessage with 3s timeout (e.g. when the target process is stopped in the debugger)
				if (NativeMethods.SendMessageTimeout(
					hWnd, NativeMethods.WM_COPYDATA, IntPtr.Zero, ref lParam,
					SMTO_NORMAL, 3000, out result) != IntPtr.Zero)
				{
					return result;
				} else {
					return IntPtr.Zero;
				}
			}
		}
		#endregion
		
		void Window_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			if (e.Uri.Scheme == "resource") {
				AvalonEditTextOutput output = new AvalonEditTextOutput();
				using (Stream s = typeof(App).Assembly.GetManifestResourceStream(typeof(App), e.Uri.AbsolutePath)) {
					using (StreamReader r = new StreamReader(s)) {
						string line;
						while ((line = r.ReadLine()) != null) {
							output.Write(line);
							output.WriteLine();
						}
					}
				}
				ILSpy.MainWindow.Instance.TextView.ShowText(output);
			}
		}
	}
}