(*** hide ***)
#I "../../bin"

(**

# A Tale of 3 Nightclubs

This C# tutorial is based on [Scalaz tutorial](https://gist.github.com/oxbowlakes/970717) by Chris Marshall and was originally ported to [fsharpx](https://github.com/fsprojects/fsharpx/blob/master/tests/FSharpx.CSharpTests/ValidationExample.cs) by Mauricio Scheffer.

Additional resources:

* Railway Oriented Programming - A functional approach to error handling
	* [Blog post](http://fsharpforfunandprofit.com/posts/recipe-part2/)
    * [Slide deck](http://www.slideshare.net/ScottWlaschin/railway-oriented-programming)
    * [Video](https://vimeo.com/97344498)

## Part Zero : 10:15 Saturday Night

We start by referencing Chessie and opening the ErrorHandling module and define a simple domain for nightclubs:

*)

#r "Chessie.dll"

open Chessie.ErrorHandling

type Sobriety = 
    | Sober
    | Tipsy
    | Drunk
    | Paralytic
    | Unconscious

type Gender = 
    | Male
    | Female

type Person = 
    { Gender : Gender
      Age : int
      Clothes : string Set
      Sobriety : Sobriety }

(**
Now we define some validation methods that all nightclubs will perform:
*)

module Club = 
    let checkAge (p : Person) = 
        if p.Age < 18 then fail "Too young!"
        elif p.Age > 40 then fail "Too old!"
        else ok p
    
    let checkClothes (p : Person) = 
        if p.Gender = Male && not (p.Clothes.Contains "Tie") then fail "Smarten up!"
        elif p.Gender = Female && p.Clothes.Contains "Trainers" then fail "Wear high heels"
        else ok p
    
    let checkSobriety (p : Person) = 
        match p.Sobriety with
        | Drunk | Paralytic | Unconscious -> fail "Sober up!"
        | _ -> ok p

(**
## Part One : Clubbed to Death

Now let's compose some validation checks via syntactic sugar:
*)

module ClubbedToDeath =
    open Club
    
    let costToEnter p =
        trial {
            let! a = checkAge p
            let! b = checkClothes a
            let! c = checkSobriety b
            return 
                match c.Gender with
                | Female -> 0m
                | Male -> 5m
        }

(**
Let's see how the validation works in action:
*)

let Ken = { Person.Gender = Male; Age = 28; Clothes = set ["Tie"; "Shirt"]; Sobriety = Tipsy }
let Dave = { Person.Gender = Male; Age = 41; Clothes = set ["Tie"; "Jeans"]; Sobriety = Sober }
let Ruby = { Person.Gender = Female; Age = 25; Clothes = set ["High heels"]; Sobriety = Tipsy }

ClubbedToDeath.costToEnter Dave 
// [fsi:val it : Chessie.ErrorHandling.Result<decimal,string> = Fail ["Too old!"]]

ClubbedToDeath.costToEnter Ken
// [fsi:val it : Result<decimal,string> = Ok (5M,[])]

ClubbedToDeath.costToEnter Ruby
// [fsi:val it : Result<decimal,string> = Ok (0M,[])

ClubbedToDeath.costToEnter { Ruby with Age = 17 } 
// [fsi:val it : Chessie.ErrorHandling.Result<decimal,string> = Fail ["Too young!"]]

(**
The thing to note here is how the Validations can be composed together in a computation expression.
The type system is making sure that failures flow through your computation in a safe manner.
*)