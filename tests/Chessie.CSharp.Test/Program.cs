using System;
using System.Reflection;

namespace Chessie.CSharp.TestRunner
{
    public static class Program
    {
        public static int Main(string[] argv)
        {
#if NETSTANDARD1_5
            var run = new NUnitLite.AutoRun(typeof(Program).GetTypeInfo().Assembly);
            return run.Execute(argv, (new NUnit.Common.ExtendedTextWrapper(Console.Out)), Console.In);
#else
            var run = new NUnitLite.AutoRun();
            return run.Execute(argv);
#endif
        }
    }
}
