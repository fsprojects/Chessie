/// Contains error propagation functions and a computation expression builder for Railway-oriented programming.
namespace Chessie.ErrorHandling

open System

 
/// Represents the result of a computation.
type Result<'TSuccess, 'TMessage> = 
    /// Represents the result of a successful computation.
    | Ok of 'TSuccess * 'TMessage list
    /// Represents the result of a failed computation.
    | Fail of 'TMessage list

    static member FailWith(msgs:'TMessage seq) : Result<'TSuccess, 'TMessage> = Result<'TSuccess, 'TMessage>.Fail(msgs |> Seq.toList)
    static member FailWith(msgs:'TMessage) : Result<'TSuccess, 'TMessage> = Result<'TSuccess, 'TMessage>.Fail([msgs])
    
    static member Succeed(x:'TSuccess) : Result<'TSuccess, 'TMessage> = Result<'TSuccess, 'TMessage>.Ok(x,[])
    static member Succeed(x:'TSuccess,message:'TMessage) : Result<'TSuccess, 'TMessage> = Result<'TSuccess, 'TMessage>.Ok(x,[message])
    static member Succeed(x:'TSuccess,messages:'TMessage seq) : Result<'TSuccess, 'TMessage> = Result<'TSuccess, 'TMessage>.Ok(x,messages |> Seq.toList)

    override this.ToString() =
        match this with
        | Ok(v,msgs) -> sprintf "OK: %A - %s" v (String.Join(Environment.NewLine, msgs |> Seq.map (fun x -> x.ToString())))
        | Fail(msgs) -> sprintf "Error: %s" (String.Join(Environment.NewLine, msgs |> Seq.map (fun x -> x.ToString())))
    
[<AutoOpen>]
module Operators =       
    /// Wraps a value in a Success
    let inline ok<'a,'b> (x:'a) : Result<'a,'b> = Ok(x, [])

    /// Wraps a message in a Failure
    let inline fail<'a,'b> (msg:'b) : Result<'a,'b> = Fail([ msg ])

    /// Returns true if the result was not successful.
    let inline failed result = 
        match result with
        | Fail _ -> true
        | _ -> false

    /// Takes a Result and maps it with fSuccess if it is a Success otherwise it maps it with fFailure.
    let inline either fSuccess fFailure trialResult = 
        match trialResult with
        | Ok(x, msgs) -> fSuccess (x, msgs)
        | Fail(msgs) -> fFailure (msgs)

    /// If the given result is a Success the wrapped value will be returned. 
    ///Otherwise the function throws an exception with Failure message of the result.
    let inline returnOrFail result = 
        let inline raiseExn msgs = 
            msgs
            |> Seq.map (sprintf "%O")
            |> String.concat (Environment.NewLine + "\t")
            |> failwith
        either fst raiseExn result

    /// Appends the given messages with the messages in the given result.
    let inline mergeMessages msgs result = 
        let inline fSuccess (x, msgs2) = Ok(x, msgs @ msgs2)
        let inline fFailure errs = Fail(errs @ msgs)
        either fSuccess fFailure result

    /// If the result is a Success it executes the given function on the value.
    /// Otherwise the exisiting failure is propagated.
    let inline bind f result = 
        let inline fSuccess (x, msgs) = f x |> mergeMessages msgs
        let inline fFailure (msgs) = Fail msgs
        either fSuccess fFailure result

   /// Flattens a nested result given the Failure types are equal
    let inline flatten (result : Result<Result<_,_>,_>) =
        result |> bind (fun x -> x)

    /// If the result is a Success it executes the given function on the value. 
    /// Otherwise the exisiting failure is propagated.
    /// This is the infix operator version of ErrorHandling.bind
    let inline (>>=) result f = bind f result

    /// If the wrapped function is a success and the given result is a success the function is applied on the value. 
    /// Otherwise the exisiting error messages are propagated.
    let inline apply wrappedFunction result = 
        match wrappedFunction, result with
        | Ok(f, msgs1), Ok(x, msgs2) -> Ok(f x, msgs1 @ msgs2)
        | Fail errs, Ok(_, msgs) -> Fail(errs @ msgs)
        | Ok(_, msgs), Fail errs -> Fail(errs @ msgs)
        | Fail errs1, Fail errs2 -> Fail(errs1 @ errs2)

    /// If the wrapped function is a success and the given result is a success the function is applied on the value. 
    /// Otherwise the exisiting error messages are propagated.
    /// This is the infix operator version of ErrorHandling.apply
    let inline (<*>) wrappedFunction result = apply wrappedFunction result

    /// Lifts a function into a Result container and applies it on the given result.
    let inline lift f result = apply (ok f) result

    /// Lifts a function into a Result and applies it on the given result.
    /// This is the infix operator version of ErrorHandling.lift
    let inline (<!>) f result = lift f result

    /// If the result is a Success it executes the given success function on the value and the messages.
    /// If the result is a Failure it executes the given failure function on the messages.
    /// Result is propagated unchanged.
    let inline eitherTee fSuccess fFailure result =
        let inline tee f x =
            f x
            x
        tee (either fSuccess fFailure) result

    /// If the result is a Success it executes the given function on the value and the messages.
    /// Result is propagated unchanged.
    let inline successTee f result = 
        eitherTee f ignore result

    /// If the result is a Failure it executes the given function on the messages.
    /// Result is propagated unchanged.
    let inline failureTee f result = 
        eitherTee ignore f result

    /// Collects a sequence of Results and accumulates their values.
    /// If the sequence contains an error the error will be propagated.
    let inline collect xs = 
        Seq.fold (fun result next -> 
            match result, next with
            | Ok(rs, m1), Ok(r, m2) -> Ok(r :: rs, m1 @ m2)
            | Ok(_, m1), Fail(m2) | Fail(m1), Ok(_, m2) -> Fail(m1 @ m2)
            | Fail(m1), Fail(m2) -> Fail(m1 @ m2)) (ok []) xs
        |> lift (List.rev >> List.toSeq)

    /// Converts an option into a Result.
    let inline failIfNone message result = 
        match result with
        | Some x -> ok x
        | None -> fail message

    /// Builder type for error handling computation expressions.
    type ErrorHandlingBuilder() = 
        member __.Zero() = ok()
        member __.Bind(m, f) = bind f m
        member __.Return(x) = ok x
        member __.ReturnFrom(x) = x
        member __.Combine (a, b) = bind b a
        member __.Delay f = f
        member __.Run f = f ()
        member __.TryWith (body, handler) =
            try
                body()
            with
            | e -> handler e
        member __.TryFinally (body, compensation) =
            try
                body()
            finally
                compensation()
        member x.Using(d:#IDisposable, body) =
            let result = fun () -> body d
            x.TryFinally (result, fun () ->
                match d with
                | null -> ()
                | d -> d.Dispose())
        member x.While (guard, body) =
            if not <| guard () then
                x.Zero()
            else
                bind (fun () -> x.While(guard, body)) (body())
        member x.For(s:seq<_>, body) =
            x.Using(s.GetEnumerator(), fun enum ->
                x.While(enum.MoveNext,
                    x.Delay(fun () -> body enum.Current)))

    /// Wraps computations in an error handling computation expression.
    let trial = ErrorHandlingBuilder()

type AsyncResult<'a, 'b> = 
    | AR of Async<Result<'a, 'b>>

[<AutoOpen>]
module AsyncExtensions = 
    [<RequireQualifiedAccess>]
    module Async = 
        let singleton value = value |> async.Return
        let bind f x = async.Bind(x, f)
        let map f x = x |> bind (f >> singleton)
        let ofAsyncResult (AR x) = x

[<AutoOpen>]
module AsyncTrial = 
    type AsyncTrialBuilder() = 
        
        member __.Return value : AsyncResult<'a, 'b> = 
            value
            |> ok
            |> Async.singleton
            |> AR
        
        member __.ReturnFrom(asyncResult : AsyncResult<'a, 'b>) = asyncResult
        member this.Zero() : AsyncResult<unit, 'b> = this.Return()
        member __.Delay(generator : unit -> AsyncResult<'a, 'b>) : AsyncResult<'a, 'b> = 
            async.Delay(generator >> Async.ofAsyncResult) |> AR
        
        member __.Bind(asyncResult : AsyncResult<'a, 'c>, binder : 'a -> AsyncResult<'b, 'c>) : AsyncResult<'b, 'c> = 
            let fSuccess (value, msgs) = 
                value |> (binder
                          >> Async.ofAsyncResult
                          >> Async.map (mergeMessages msgs))
            
            let fFailure errs = 
                errs
                |> Fail
                |> Async.singleton
            
            asyncResult
            |> Async.ofAsyncResult
            |> Async.bind (either fSuccess fFailure)
            |> AR
        
        member this.Bind(result : Result<'a, 'c>, binder : 'a -> AsyncResult<'b, 'c>) : AsyncResult<'b, 'c> = 
            this.Bind(result
                      |> Async.singleton
                      |> AR, binder)
        
        member __.Bind(async : Async<'a>, binder : 'a -> AsyncResult<'b, 'c>) : AsyncResult<'b, 'c> = 
            async
            |> Async.bind (binder >> Async.ofAsyncResult)
            |> AR
        
        member __.TryWith(asyncResult : AsyncResult<'a, 'b>, catchHandler : exn -> AsyncResult<'a, 'b>) : AsyncResult<'a, 'b> = 
            async.TryWith(asyncResult |> Async.ofAsyncResult, (catchHandler >> Async.ofAsyncResult)) |> AR
        member __.TryFinally(asyncResult : AsyncResult<'a, 'b>, compensation : unit -> unit) : AsyncResult<'a, 'b> = 
            async.TryFinally(asyncResult |> Async.ofAsyncResult, compensation) |> AR
        member __.Using(resource : 'T when 'T :> System.IDisposable, binder : 'T -> AsyncResult<'a, 'b>) : AsyncResult<'a, 'b> = 
            async.Using(resource, (binder >> Async.ofAsyncResult)) |> AR
    
    let asyncTrial = AsyncTrialBuilder()

namespace Chessie.ErrorHandling.CSharp

open System
open System.Runtime.CompilerServices
open Chessie.ErrorHandling

[<Extension>]
type ResultExtensions () =
    [<Extension>]
    /// Allows pattern matching on Results from C#.
    static member inline Match(value, ifSuccess:Action<'a , ('b list)>, ifFailure:Action<'b list>) =
        match value with
        | Ok(x, msgs) -> ifSuccess.Invoke(x,msgs)
        | Fail(msgs) -> ifFailure.Invoke(msgs)

    [<Extension>]
    /// Lifts a Func into a Result and applies it on the given result.
    static member inline Map(value,func:Func<_,_>) =
        lift func.Invoke value

    [<Extension>]
    /// Collects a sequence of Results and accumulates their values.
    /// If the sequence contains an error the error will be propagated.
    static member inline Collect(values) =
        collect values

    [<Extension>]
    /// Collects a sequence of Results and accumulates their values.
    /// If the sequence contains an error the error will be propagated.
    static member inline Collect(value) =
        match value with
        | Ok(xs, msgs) -> collect xs
        | Fail(msgs) -> fail msgs