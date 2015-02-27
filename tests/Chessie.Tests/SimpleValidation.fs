module Chessie.Tests

open Chessie.ErrorHandling
open NUnit.Framework
open FsUnit

type Request = 
    { Name : string
      EMail : string }

let validateInput input = 
    if input.Name = "" then fail "Name must not be blank"
    elif input.EMail = "" then fail "Email must not be blank"
    else succeed input // happy path

let validate1 input = 
    if input.Name = "" then fail "Name must not be blank"
    else succeed input

let validate2 input = 
    if input.Name.Length > 50 then fail "Name must not be longer than 50 chars"
    else succeed input

let validate3 input = 
    if input.EMail = "" then fail "Email must not be blank"
    else succeed input

let combinedValidation = 
    // connect the two-tracks together
    validate1
    >> bind validate2
    >> bind validate3

[<Test>]
let ``should find empty name``() = 
    { Name = ""
      EMail = "" }
    |> combinedValidation
    |> shouldEqual (Failure [ "Name must not be blank" ])

[<Test>]
let ``should find empty mail``() = 
    { Name = "Scott"
      EMail = "" }
    |> combinedValidation
    |> shouldEqual (Failure [ "Email must not be blank" ])

[<Test>]
let ``should find long name``() = 
    { Name = "ScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScott"
      EMail = "" }
    |> combinedValidation
    |> shouldEqual (Failure [ "Name must not be longer than 50 chars" ])

[<Test>]
let ``should not complain on valid data``() = 
    let scott = 
        { Name = "Scott"
          EMail = "scott@chessie.com" }
    scott
    |> combinedValidation
    |> returnOrFail
    |> shouldEqual scott

let canonicalizeEmail input = { input with EMail = input.EMail.Trim().ToLower() }

let usecase = 
    validate1
    >> bind validate2
    >> bind validate3
    >> (lift canonicalizeEmail)

[<Test>]
let ``should canonicalize valid data``() = 
    { Name = "Scott"
      EMail = "SCOTT@CHESSIE.com" }
    |> usecase
    |> returnOrFail
    |> shouldEqual { Name = "Scott"
                     EMail = "scott@chessie.com" }

[<Test>]
let ``should not canonicalize invalid data``() = 
    { Name = ""
      EMail = "SCOTT@CHESSIE.com" }
    |> usecase
    |> shouldEqual (Failure [ "Name must not be blank" ])
