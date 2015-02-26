namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Chessie")>]
[<assembly: AssemblyProductAttribute("Chessie")>]
[<assembly: AssemblyDescriptionAttribute("Railway-oriented programming for .NET")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
