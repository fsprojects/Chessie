module Chessie.Validaton.NightClubs.Tests

open Chessie.ErrorHandling
open NUnit.Framework
open FsUnit

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

// Let's define the checks that *all* nightclubs make!
module Club = 
    let checkAge (p : Person) = 
        if p.Age < 18 then fail "Too young!"
        elif p.Age > 40 then fail "Too old!"
        else pass p
    
    let checkClothes (p : Person) = 
        if p.Gender = Male && not (p.Clothes.Contains "Tie") then fail "Smarten up!"
        elif p.Gender = Female && p.Clothes.Contains "Trainers" then fail "Wear high heels"
        else pass p
    
    let checkSobriety (p : Person) = 
        match p.Sobriety with
        | Drunk | Paralytic | Unconscious -> fail "Sober up!"
        | _ -> pass p

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

let Ken = { Person.Gender = Male; Age = 28; Clothes = set ["Tie"; "Shirt"]; Sobriety = Tipsy }
let Dave = { Person.Gender = Male; Age = 41; Clothes = set ["Tie"; "Jeans"]; Sobriety = Sober }
let Ruby = { Person.Gender = Female; Age = 25; Clothes = set ["High heels"]; Sobriety = Tipsy }

[<Test>]
let part1() =
    ClubbedToDeath.costToEnter Dave |> shouldEqual (fail "Too old!")
    ClubbedToDeath.costToEnter Ken |> shouldEqual (pass 5m)
    ClubbedToDeath.costToEnter Ruby |> shouldEqual (pass 0m)
    ClubbedToDeath.costToEnter { Ruby with Age = 17 } |> shouldEqual (fail "Too young!")
    ClubbedToDeath.costToEnter { Ken with Sobriety = Unconscious } |> shouldEqual (fail "Sober up!")