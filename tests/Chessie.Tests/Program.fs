module Chessie.TestRunner

open System
open System.Reflection

type Program = class end

[<EntryPoint>]
let main argv = 

#if NETSTANDARD1_5
    let run = typeof<Program>.GetTypeInfo().Assembly |> NUnitLite.AutoRun
    run.Execute(argv, (new NUnit.Common.ExtendedTextWrapper(Console.Out)), Console.In)
#else
    let run = NUnitLite.AutoRun()
    run.Execute(argv)
#endif
