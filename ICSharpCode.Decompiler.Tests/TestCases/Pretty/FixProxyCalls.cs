﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	internal class A
	{
		protected internal virtual Task<string> Test(string test)
		{
			return Task.Run(() => test.ToUpper());
		}
	}

	internal class B : A
	{
		protected internal override async Task<string> Test(string test)
		{
			return await base.Test(test);
		}
	}

	internal class B2<T> : A
	{
		protected internal override async Task<string> Test(string test)
		{
			return await base.Test(test);
		}
	}

	internal class C
	{
		protected internal virtual string Test(string test)
		{
			return string.Join(test, "fsdf");
		}
	}

	internal class D : C
	{
		protected internal IEnumerable<string> Test2(string test)
		{
			yield return base.Test(test);
		}
	}

	internal class E
	{
		protected internal virtual string Test(string test)
		{
			return string.Join(test, "fsdf");
		}
	}

	internal class F : E
	{
		protected internal override string Test(string test)
		{
			Func<string, string> func = (string a) => base.Test(a);
			test = string.Join(test, "aa");
			return func(test);
		}
	}

	internal class G
	{
		protected internal virtual void Test(string test)
		{
			string.Join(test, "fsdf");
		}
	}

	internal class H : G
	{
		protected internal override void Test(string test)
		{
			Action<string> action = delegate(string a) {
				base.Test(a);
			};
			if (test.Equals(1)) {
				throw new Exception("roslyn optimize is inlining the assignment which lets the test fail");
			}
			action(test);
		}
	}

	internal class I
	{
		protected internal virtual void Test(int a)
		{

		}
	}

	internal class J : I
	{
		protected internal override void Test(int a)
		{
			Action action = delegate {
				base.Test(a);
			};
			if (a.Equals(1)) {
				throw new Exception("roslyn optimize is inlining the assignment which lets the test fail");
			}
			action();

		}
	}

	internal class K
	{
		protected internal virtual IEnumerable<int> Test(int p)
		{
			yield return p + 1;
			yield return p + 2;
		}
	}

	internal class L : K
	{
		protected internal override IEnumerable<int> Test(int p)
		{
			yield return base.Test(base.Test(0).GetEnumerator().Current).GetEnumerator().Current;
		}
	}
}