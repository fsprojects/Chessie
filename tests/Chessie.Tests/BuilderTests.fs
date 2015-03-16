module Chessie.Builder.Tests

open Chessie.ErrorHandling
open NUnit.Framework
open FsUnit
open System

[<Test>]
let ``Using CE syntax should be equivilent to bind`` () =
    let sut =
        trial {
            let! bob = ok "bob"
            let greeting = sprintf "Hello %s" bob
            return greeting
        }
    sut |> shouldEqual (bind (sprintf "Hello %s" >> ok) (ok "bob"))

[<Test>]
let ``You should be able to "combine" in CE syntax`` () =
    let sut =
        trial {
            if "bob" = "bob" then
                do! ok ()
            return "bob"
        }
    sut |> shouldEqual (ok "bob")

[<Test>]
let ``Try .. with works in CE syntax`` () =
    let sut =
        trial {
            return
                try
                    failwith "bang"
                    "not bang"
                with
                | e -> e.Message
        }
    sut |> shouldEqual (ok "bang")

[<Test>]
let ``Try .. finally works in CE syntax`` () =
    let i = ref 0
    try
        trial {
            try
                failwith "bang"
            finally
                i := 1
        }
    with
    | e -> ok ()
    |> returnOrFail
    !i |> shouldEqual 1

[<Test>]
let ``use! works in CE expressions`` () =
    use mem = new IO.MemoryStream()
    try
        trial {
            use! mem = ok <| new IO.StreamReader(mem)
            failwith "bang"
        }
    with
    | e -> ok ()
    |> returnOrFail
    (fun () -> mem.ReadByte() |> ignore) |> shouldFail<ObjectDisposedException>
    
[<Test>]
let ``While works in CE syntax`` () =
    let i = ref 0
    trial {
        while !i < 3 do
            i := !i + 1
    }
    |> returnOrFail
    !i |> shouldEqual 3

[<Test>]
let ``For works in CE syntax`` () =
    let i = ref 0
    trial {
        for x in 0..2 do
            i := !i + x
    }
    |> returnOrFail
    !i |> shouldEqual 3