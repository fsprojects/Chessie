using System;
using System.Reflection;

namespace Chessie.CSharp.TestRunner
{
    public static class Program
    {
        public static int Main(string[] argv)
        {
#if NETCOREAPP1_0
            return 0;
#else
            var run = new NUnitLite.AutoRun();
            return run.Execute(argv);
#endif
        }
    }
}
