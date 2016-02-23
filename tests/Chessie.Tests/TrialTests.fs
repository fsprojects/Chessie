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

[<Test>]
let ``mapFailure if success should discard warning`` () =
    Ok (42,[1;2;3])
    |> Trial.mapFailure (fun _ -> ["err1"])
    |> shouldEqual (Ok (42,[]))

[<Test>]
let ``mapFailure if failure should map over error`` () =
    fail "error"
    |> Trial.mapFailure (fun _ -> [42])
    |> shouldEqual (Bad [42])

[<Test>]
let ``mapFailure if failure should map over list of errors`` () =
    Bad ["err1"; "err2"]
    |> Trial.mapFailure (fun errs -> errs |> List.map (function "err1" -> 42 | "err2" -> 43 | _ -> 0))
    |> shouldEqual (Bad [42; 43])

[<Test>]
let ``mapFailure if failure should replace errors with singleton list`` () =
    Bad ["err1", "err2"]
    |> Trial.mapFailure (fun _ -> [42])
    |> shouldEqual (Bad [42])

[<Test>]
let ``mapFailure if failure should map over empty list of errors`` () =
    Bad []
    |> Trial.mapFailure (fun errs -> errs |> List.map (function "err1" -> 42 | "err2" -> 43 | _ -> 0))
    |> shouldEqual (Bad [])

[<Test>]
let ``tryCatch if failure should return exception`` () = 
    let ex = exn "error" 
    1 
    |> Trial.tryCatch (fun x -> raise ex) 
    |> shouldEqual (Bad[ex])
        

[<Test>]
let ``tryCatch if success should return list`` () = 
    1 
    |> Trial.tryCatch id 
    |> shouldEqual (Ok(1,[]))