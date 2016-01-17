module Chessie.Trial.Tests

open Chessie.ErrorHandling
open NUnit.Framework
open FsUnit

[<Test>]
let ``ofChoice if Choice1Of2 it should succeed`` () =
    let choice = Choice1Of2 "foo"
    let result = choice |> Trial.ofChoice
    result |> shouldEqual (ok "foo")

[<Test>]
let ``ofChoice if Choice2Of2 it should fail`` () =
    let choice = Choice2Of2 "error"
    let result = choice |> Trial.ofChoice
    result |> shouldEqual (fail "error")

[<Test>]
let ``ofChoice if Choice2Of2 of list it should fail`` () =
    let choice = Choice2Of2 ["error1";"error2"]
    let result = choice |> Trial.ofChoice
    result |> shouldEqual (fail ["error1";"error2"])