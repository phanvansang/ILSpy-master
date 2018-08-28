﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Util;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.AddIn
{
	public class AssemblyFileFinder
	{
		public static string FindAssemblyFile(AssemblyDefinition assemblyDefinition, string assemblyFile)
		{
			var assemblyName = assemblyDefinition.Name;

			var detectedTargetFramework = DetectTargetFrameworkId(assemblyDefinition, assemblyFile);
			if (string.IsNullOrEmpty(detectedTargetFramework)) {
				// Without a target framework id it makes no sense to continue
				return null;
			}

			var targetFramework = detectedTargetFramework.Split(new[] { ",Version=v" }, StringSplitOptions.None);
			string file = null;
			switch (targetFramework[0]) {
				case ".NETCoreApp":
				case ".NETStandard":
					if (targetFramework.Length != 2)
						return FindAssemblyFromGAC(assemblyDefinition);
					var version = targetFramework[1].Length == 3 ? new Version(targetFramework[1] + ".0") : new Version(targetFramework[1]);
					var dotNetCorePathFinder = new DotNetCorePathFinder(assemblyFile, detectedTargetFramework, version);
					file = dotNetCorePathFinder.TryResolveDotNetCore(Decompiler.Metadata.AssemblyNameReference.Parse(assemblyName.FullName));
					if (file != null)
						return file;
					return FindAssemblyFromGAC(assemblyDefinition);
				default:
					return FindAssemblyFromGAC(assemblyDefinition);
			}
		}

		static string FindAssemblyFromGAC(AssemblyDefinition assemblyDefinition)
		{
			return GacInterop.FindAssemblyInNetGac(Decompiler.Metadata.AssemblyNameReference.Parse(assemblyDefinition.Name.FullName));
		}

		static readonly string RefPathPattern = @"NuGetFallbackFolder[/\\][^/\\]+[/\\][^/\\]+[/\\]ref[/\\]";

		public static bool IsReferenceAssembly(AssemblyDefinition assemblyDef, string assemblyFile)
		{
			if (assemblyDef.CustomAttributes.Any(ca => ca.AttributeType.FullName == "System.Runtime.CompilerServices.ReferenceAssemblyAttribute"))
				return true;

			// Try to detect reference assembly through specific path pattern
			var refPathMatch = Regex.Match(assemblyFile, RefPathPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
			return refPathMatch.Success;
		}

		static readonly string DetectTargetFrameworkIdRefPathPattern =
			@"(Reference Assemblies[/\\]Microsoft[/\\]Framework[/\\](?<1>.NETFramework)[/\\]v(?<2>[^/\\]+)[/\\])" +
			@"|(NuGetFallbackFolder[/\\](?<1>[^/\\]+)\\(?<2>[^/\\]+)([/\\].*)?[/\\]ref[/\\])";

		public static string DetectTargetFrameworkId(AssemblyDefinition assembly, string assemblyPath = null)
		{
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));

			const string TargetFrameworkAttributeName = "System.Runtime.Versioning.TargetFrameworkAttribute";

			foreach (var attribute in assembly.CustomAttributes) {
				if (attribute.AttributeType.FullName != TargetFrameworkAttributeName)
					continue;
				if (attribute.HasConstructorArguments) {
					if (attribute.ConstructorArguments[0].Value is string value)
						return value;
				}
			}

			// Optionally try to detect target version through assembly path as a fallback (use case: reference assemblies)
			if (assemblyPath != null) {
				/*
				 * Detected path patterns (examples):
				 * 
				 * - .NETFramework -> C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1\mscorlib.dll
				 * - .NETCore      -> C:\Program Files\dotnet\sdk\NuGetFallbackFolder\microsoft.netcore.app\2.1.0\ref\netcoreapp2.1\System.Console.dll
				 * - .NETStandard  -> C:\Program Files\dotnet\sdk\NuGetFallbackFolder\netstandard.library\2.0.3\build\netstandard2.0\ref\netstandard.dll
				 */
				var pathMatch = Regex.Match(assemblyPath, DetectTargetFrameworkIdRefPathPattern,
					RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
				if (pathMatch.Success) {
					var type = pathMatch.Groups[1].Value;
					var version = pathMatch.Groups[2].Value;

					if (type == ".NETFramework") {
						return $".NETFramework,Version=v{version}";
					} else if (type.Contains("netcore")) {
						return $".NETCoreApp,Version=v{version}";
					} else if (type.Contains("netstandard")) {
						return $".NETStandard,Version=v{version}";
					}
				}
			}

			return string.Empty;
		}
	}
}
