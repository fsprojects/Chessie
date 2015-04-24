(*** hide ***)
#I "../../bin"

(**

Using Chessie for Railway-oriented programming (ROP)
====================================================

This tutorial is based on an article about [Railway-oriented programming](http://fsharpforfunandprofit.com/posts/recipe-part2/) by Scott Wlaschin.

Additional resources:

* Railway Oriented Programming by Scott Wlaschin - A functional approach to error handling 
    * [Slide deck](http://www.slideshare.net/ScottWlaschin/railway-oriented-programming)
    * [Video](https://vimeo.com/97344498)

We start by referencing Chessie and opening the ErrorHandling module:
*)

#r "Chessie.dll"

open Chessie.ErrorHandling

(**
 Now we define some simple validation functions:
*)

type Request = 
    { Name : string
      EMail : string }

let validateInput input = 
    if input.Name = "" then fail "Name must not be blank"
    elif input.EMail = "" then fail "Email must not be blank"
    else pass input // happy path

let validate1 input = 
    if input.Name = "" then fail "Name must not be blank"
    else pass input

let validate2 input = 
    if input.Name.Length > 50 then fail "Name must not be longer than 50 chars"
    else pass input

let validate3 input = 
    if input.EMail = "" then fail "Email must not be blank"
    else pass input

let combinedValidation = 
    // connect the two-tracks together
    validate1
    >> bind validate2
    >> bind validate3

(**
 Let's use these with some basic combinators:
*)

{ Name = ""; EMail = "" }
|> combinedValidation
// [fsi:val it : Chessie.ErrorHandling.Result<Request,string> =]
// [fsi:  Bad ["Name must not be blank"]]
    
{ Name = "Scott"; EMail = "" }
|> combinedValidation
// [fsi:val it : Chessie.ErrorHandling.Result<Request,string> =]
// [fsi:  bad ["Email must not be blank"]]

{ Name = "ScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScottScott"
  EMail = "" }
|> combinedValidation
// [fsi:val it : Chessie.ErrorHandling.Result<Request,string> =]
// [fsi:  Bad ["Name must not be longer than 50 chars" ]]

{ Name = "Scott"; EMail = "scott@chessie.com" }
|> combinedValidation
|> returnOrRaise
// [fsi:val it : Request = {Name = "Scott"; EMail = "scott@chessie.com";}]


let canonicalizeEmail input = { input with EMail = input.EMail.Trim().ToLower() }

let usecase = 
    combinedValidation
    >> (map canonicalizeEmail)

{ Name = "Scott"; EMail = "SCOTT@CHESSIE.com" }
|> usecase
|> returnOrRaise
// [fsi:val it : Request = {Name = "Scott"; EMail = "scott@chessie.com";}]

{ Name = ""; EMail = "SCOTT@CHESSIE.com" }
|> usecase
// [fsi:val it : Result<Request,string> = Bad ["Name must not be blank"]]    

// a dead-end function    
let updateDatabase input =
   ()   // dummy dead-end function for now


let log twoTrackInput = 
    let success(x,msgs) = printfn "DEBUG. Success so far."
    let failure msgs = printf "ERROR. %A" msgs
    eitherTee success failure twoTrackInput 

let usecase2 = 
    usecase
    >> (passTee updateDatabase)
    >> log


{ Name = "Scott"; EMail = "SCOTT@CHESSIE.com" }
|> usecase2
|> returnOrRaise
// [fsi:DEBUG. Success so far.]
// [fsi:val it : Request = {Name = "Scott";]
// [fsi:                    EMail = "scott@chessie.com";}]
