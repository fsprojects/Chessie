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

/// If the result is a Success it executes the given function on the value. Otherwise the exisiting failure is propagated.
/// Infix version of Rop.bind
let inline (>>=) result f = bind f result

/// If the wrapped function is a success and the given result is a success the function is applied on the value. Otherwise the exisiting error messages are propagated.
let inline apply wrappedFunction result = 
    match wrappedFunction, result with
    | Success(f, msgs1), Success(x, msgs2) -> Success(f x, msgs1 @ msgs2)
    | Failure errs, Success(_, msgs) -> Failure(errs @ msgs)
    | Success(_, msgs), Failure errs -> Failure(errs @ msgs)
    | Failure errs1, Failure errs2 -> Failure(errs1 @ errs2)

/// If the wrapped function is a success and the given result is a success the function is applied on the value. Otherwise the exisiting error messages are propagated.
/// Infix version of Rop.apply
let inline (<*>) wrappedFunction result = apply wrappedFunction result

/// Lifts a function into a RopResult and applies it on the given result.
let inline lift f result = apply (succeed f) result

/// Lifts a function into a RopResult and applies it on the given result.
/// Infix version of Rop.lift
let inline (<!>) f result = lift f result 

/// If the result is a Success it executes the given function on the value and the messages. Otherwise the exisiting failure is propagated.
let inline successTee f result = 
    let inline fSuccess (x,msgs) = 
        f (x,msgs)
        Success (x,msgs) 
    let inline fFailure errs = Failure errs 
    either fSuccess fFailure result

/// If the result is a Failure it executes the given function on the value and the messages. Otherwise the exisiting successful value is propagated.
let inline failureTee f result = 
    let inline fSuccess (x,msgs) = Success (x,msgs) 
    let inline fFailure errs = 
        f errs
        Failure errs 
    either fSuccess fFailure result

/// Collects a sequence of RopResults and accumulates their values. IF the sequence contains an error the error will be propagated.
let inline collect xs = 
    Seq.fold (fun result next -> 
        match result, next with
        | Success(rs, m1), Success(r, m2) -> Success(r :: rs, m1 @ m2)
        | Success(_, m1), Failure(m2) | Failure(m1), Success(_, m2) -> Failure(m1 @ m2)
        | Failure(m1), Failure(m2) -> Failure(m1 @ m2)) (succeed []) xs
    |> lift List.rev

/// Converts an option into a RopResult.
let inline failIfNone message result =
    match result with
    | Some x -> succeed x
    | None -> fail message

/// Builder type for railway-oriented computation expressions.
type RopBuilder() =
    member __.Zero() = succeed ()
    member __.Bind(m, f) = bind f m
    member __.Return(x) = succeed x
    member __.ReturnFrom(x) = x

/// Railway-oriented computation expressions.
let rop = RopBuilder()