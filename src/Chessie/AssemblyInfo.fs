namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Chessie")>]
[<assembly: AssemblyProductAttribute("Chessie")>]
[<assembly: AssemblyDescriptionAttribute("Railway-oriented programming for .NET")>]
[<assembly: AssemblyVersionAttribute("0.0.14")>]
[<assembly: AssemblyFileVersionAttribute("0.0.14")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.14"
