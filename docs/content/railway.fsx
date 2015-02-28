(*** hide ***)
#I "../../bin"

(**

Using Chessie for Railway-oriented programming
==============================================

Railway-oriented programming was introduced by Scott Wlaschin.

Resources:

* Railway Oriented Programming - A functional approach to error handling 
    * [Slide deck](http://www.slideshare.net/ScottWlaschin/railway-oriented-programming)
    * [Video](https://vimeo.com/97344498)

*)
#r "Chessie.dll"

open Chessie.ErrorHandling

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

{ Name = ""; EMail = "" }
|> combinedValidation
// [fsi:val it : Chessie.ErrorHandling.Result<Request,string> =]
// [fsi:  Fail ["Name must not be blank"]]
    
{ Name = "Scott"; EMail = "" }
|> combinedValidation
// [fsi:val it : Chessie.ErrorHandling.Result<Request,string> =]
// [fsi:  Fail ["Email must not be blank"]]

{ Name = "ScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScott"
  EMail = "" }
|> combinedValidation
// [fsi:val it : Chessie.ErrorHandling.Result<Request,string> =]
// [fsi:  Fail ["Name must not be longer than 50 chars" ]]

{ Name = "Scott"; EMail = "scott@chessie.com" }
|> combinedValidation
|> returnOrFail
// [fsi:val it : Request = {Name = "Scott"; EMail = "scott@chessie.com";}]


let canonicalizeEmail input = { input with EMail = input.EMail.Trim().ToLower() }

let usecase = 
    combinedValidation
    >> (lift canonicalizeEmail)

{ Name = "Scott"; EMail = "SCOTT@CHESSIE.com" }
|> usecase
|> returnOrFail
// [fsi:val it : Request = {Name = "Scott"; EMail = "scott@chessie.com";}]

{ Name = ""; EMail = "SCOTT@CHESSIE.com" }
|> usecase
// [fsi:val it : Result<Request,string> = Fail ["Name must not be blank"]]    

// a dead-end function    
let updateDatabase input =
   ()   // dummy dead-end function for now


let log twoTrackInput = 
    let success(x,msgs) = printfn "DEBUG. Success so far."; Success(x,msgs)
    let failure msgs = printf "ERROR. %A" msgs; Failure(msgs)
    either success failure twoTrackInput 

let usecase2 = 
    usecase
    >> (successTee updateDatabase)
    >> log


{ Name = "Scott"; EMail = "SCOTT@CHESSIE.com" }
|> usecase2
|> returnOrFail
// [fsi:DEBUG. Success so far.]
// [fsi:val it : Request = {Name = "Scott";]
// [fsi:                    EMail = "scott@chessie.com";}]
