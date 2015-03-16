module Chessie.Builder.Tests

open Chessie.ErrorHandling
open NUnit.Framework
open FsUnit
open System

[<Test>]
let ``Using CE syntax should be equivilent to bind`` () =
    let sut =
        trial {
            let! bob = pass "bob"
            let greeting = sprintf "Hello %s" bob
            return greeting
        }
    sut |> shouldEqual (bind (sprintf "Hello %s" >> pass) (pass "bob"))

[<Test>]
let ``You should be able to "combine" in CE syntax`` () =
    let sut =
        trial {
            if "bob" = "bob" then
                do! pass ()
            return "bob"
        }
    sut |> shouldEqual (pass "bob")

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
    sut |> shouldEqual (pass "bang")

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
    | e -> pass ()
    |> returnOrFail
    !i |> shouldEqual 1

[<Test>]
let ``use! works in CE expressions`` () =
    use mem = new IO.MemoryStream()
    try
        trial {
            use! mem = pass <| new IO.StreamReader(mem)
            failwith "bang"
        }
    with
    | e -> pass ()
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