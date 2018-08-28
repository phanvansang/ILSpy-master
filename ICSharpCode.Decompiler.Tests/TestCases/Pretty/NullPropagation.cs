﻿// Copyright (c) 2018 Daniel Grunwald
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

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class NullPropagation
	{
		private class MyClass
		{
			public int IntVal;
			public string Text;
			public MyClass Field;
			public MyClass Property {
				get;
				set;
			}
			public MyClass this[int index] {
				get {
					return null;
				}
			}
			public MyClass Method(int arg)
			{
				return null;
			}

			public void Done()
			{
			}
		}

		private struct MyStruct
		{
			public int IntVal;
			public MyClass Field;
			public MyStruct? Property1 => null;
			public MyStruct Property2 => default(MyStruct);
			public MyStruct? this[int index] {
				get {
					return null;
				}
			}
			public MyStruct? Method1(int arg)
			{
				return null;
			}
			public MyStruct Method2(int arg)
			{
				return default(MyStruct);
			}

			public void Done()
			{
			}
		}

		public interface ITest
		{
			int Int();
			ITest Next();
		}

		private int GetInt()
		{
			return 9;
		}

		private string GetString()
		{
			return null;
		}

		private MyClass GetMyClass()
		{
			return null;
		}

		private MyStruct? GetMyStruct()
		{
			return null;
		}

		public string Substring()
		{
			return GetString()?.Substring(GetInt());
		}

		public void CallSubstringAndIgnoreResult()
		{
			GetString()?.Substring(GetInt());
		}

		private void Use<T>(T t)
		{
		}

		public void CallDone()
		{
			GetMyClass()?.Done();
			GetMyClass()?.Field?.Done();
			GetMyClass()?.Field.Done();
			GetMyClass()?.Property?.Done();
			GetMyClass()?.Property.Done();
			GetMyClass()?.Method(GetInt())?.Done();
			GetMyClass()?.Method(GetInt()).Done();
			GetMyClass()?[GetInt()]?.Done();
			GetMyClass()?[GetInt()].Done();
		}

		public void CallDoneStruct()
		{
			GetMyStruct()?.Done();
			GetMyStruct()?.Field?.Done();
			GetMyStruct()?.Field.Done();
			GetMyStruct()?.Property1?.Done();
			GetMyStruct()?.Property2.Done();
			GetMyStruct()?.Method1(GetInt())?.Done();
			GetMyStruct()?.Method2(GetInt()).Done();
			GetMyStruct()?[GetInt()]?.Done();
		}

		public void RequiredParentheses()
		{
			(GetMyClass()?.Field).Done();
			(GetMyClass()?.Method(GetInt())).Done();
			(GetMyStruct()?.Property2)?.Done();
		}

		public int?[] ChainsOnClass()
		{
			return new int?[9] {
				GetMyClass()?.IntVal,
				GetMyClass()?.Field.IntVal,
				GetMyClass()?.Field?.IntVal,
				GetMyClass()?.Property.IntVal,
				GetMyClass()?.Property?.IntVal,
				GetMyClass()?.Method(GetInt()).IntVal,
				GetMyClass()?.Method(GetInt())?.IntVal,
				GetMyClass()?[GetInt()].IntVal,
				GetMyClass()?[GetInt()]?.IntVal
			};
		}

		public int?[] ChainsStruct()
		{
			return new int?[8] {
				GetMyStruct()?.IntVal,
				GetMyStruct()?.Field.IntVal,
				GetMyStruct()?.Field?.IntVal,
				GetMyStruct()?.Property2.IntVal,
				GetMyStruct()?.Property1?.IntVal,
				GetMyStruct()?.Method2(GetInt()).IntVal,
				GetMyStruct()?.Method1(GetInt())?.IntVal,
				GetMyStruct()?[GetInt()]?.IntVal
			};
		}

		public int CoalescingReturn()
		{
			return GetMyClass()?.IntVal ?? 1;
		}

		public void Coalescing()
		{
			Use(GetMyClass()?.IntVal ?? 1);
		}

		public void CoalescingString()
		{
			Use(GetMyClass()?.Text ?? "Hello");
		}

		public void InvokeDelegate(EventHandler eh)
		{
			eh?.Invoke(null, EventArgs.Empty);
		}

		public int? InvokeDelegate(Func<int> f)
		{
			return f?.Invoke();
		}

		private void NotNullPropagation(MyClass c)
		{
			// don't decompile this to "(c?.IntVal ?? 0) != 0"
			if (c != null && c.IntVal != 0) {
				Console.WriteLine("non-zero");
			}
			if (c == null || c.IntVal == 0) {
				Console.WriteLine("null or zero");
			}
			Console.WriteLine("end of method");
		}

		private void Setter(MyClass c)
		{
			if (c != null) {
				c.IntVal = 1;
			}
			Console.WriteLine();
			if (c != null) {
				c.Property = null;
			}
		}

		private static int? GenericUnconstrainedInt<T>(T t) where T : ITest
		{
			return t?.Int();
		}

		private static int? GenericClassConstraintInt<T>(T t) where T : class, ITest
		{
			return t?.Int();
		}

		private static int? GenericStructConstraintInt<T>(T? t) where T : struct, ITest
		{
			return t?.Int();
		}

		// See also: https://github.com/icsharpcode/ILSpy/issues/1050
		// The C# compiler generates pretty weird code in this case.
		//private static int? GenericRefUnconstrainedInt<T>(ref T t) where T : ITest
		//{
		//	return t?.Int();
		//}

		private static int? GenericRefClassConstraintInt<T>(ref T t) where T : class, ITest
		{
			return t?.Int();
		}

		private static int? GenericRefStructConstraintInt<T>(ref T? t) where T : struct, ITest
		{
			return t?.Int();
		}

		private static dynamic DynamicNullProp(dynamic a)
		{
			return a?.b.c(1)?.d[10];
		}
	}
}
