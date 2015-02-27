module Chessie.Rop

open System

/// Railway-oriented programming result - represents the result of a computation
type RopResult<'TSuccess, 'TMessage> =    
    /// Represents the result of a successful computation
    | Success of 'TSuccess * 'TMessage list
    /// Represents the result of a failed computation
    | Failure of 'TMessage list

/// Wraps a value in a Success
let inline succeed x = Success(x,[])

/// Wraps a message in a Failure
let inline fail msg = Failure([msg])

/// Takes a RopResult and maps it with fSuccess if it is a Success otherwise it maps it with fFailure.
let inline either fSuccess fFailure ropResult = 
    match ropResult with
    | Success(x, msgs) -> fSuccess(x,msgs)
    | Failure(msgs) -> fFailure(msgs)

/// If the given result is a Success the wrapped value will be returned. Otherwise the function throws an exception with Failure message of the result.
let inline returnOrFail result = 
    let inline raiseExn msgs = 
        msgs 
        |> Seq.map (sprintf "%O")
        |> String.concat (Environment.NewLine + "\t")
        |> failwith
    either fst raiseExn result

/// Appends the given messages with the messages in the given result.
let inline mergeMessages msgs result =
    let inline fSuccess (x,msgs2) = Success (x, msgs @ msgs2) 
    let inline fFailure errs = Failure (errs @ msgs) 
    either fSuccess fFailure result

/// If the result is a Success it executes the given function on the value. Otherwise the exisiting failure is propagated.
let inline bind f result =
    let inline fSuccess (x, msgs) = f x |> mergeMessages msgs
    let inline fFailure (msgs) = Failure msgs
    either fSuccess fFailure result      

let apply f result =
    match f,result with
    | Success (f,msgs1), Success (x,msgs2) -> 
        (f x, msgs1@msgs2) |> Success 
    | Failure errs, Success (_,msgs) 
    | Success (_,msgs), Failure errs -> 
        errs @ msgs |> Failure
    | Failure errs1, Failure errs2 -> 
        errs1 @ errs2 |> Failure 

let lift f result =
    let f' = f |> succeed
    apply f' result

let successTee f result = 
    let fSuccess (x,msgs) = 
        f (x,msgs)
        Success (x,msgs) 
    let fFailure errs = Failure errs 
    either fSuccess fFailure result

let failureTee f result = 
    let fSuccess (x,msgs) = Success (x,msgs) 
    let fFailure errs = 
        f errs
        Failure errs 
    either fSuccess fFailure result

let collect xs =
    Seq.fold (fun result next -> 
                    match result, next with
                    | Success(rs,m1), Success(r,m2) -> Success(r::rs,m1@m2)
                    | Success(_,m1), Failure(m2) 
                    | Failure(m1), Success(_,m2) -> Failure(m1@m2)
                    | Failure(m1), Failure(m2) -> Failure(m1@m2)) (succeed []) xs
    |> lift List.rev

let failIfNone message = function
    | Some x -> succeed x
    | None -> fail message 

/// infix version of Rop.bind
let (>>=) result f = bind f result

/// infix version of Rop.lift
let (<!>) = lift

/// infix version of Rop.apply
let (<*>) = apply

type RopBuilder() =
    member __.Zero() = succeed ()
    member __.Bind(m, f) = bind f m
    member __.Return(x) = succeed x
    member __.ReturnFrom(x) = x

let rop = RopBuilder()