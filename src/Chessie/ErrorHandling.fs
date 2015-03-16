/// Contains error propagation functions and a computation expression builder for Railway-oriented programming.
namespace Chessie.ErrorHandling

open System

/// Represents the result of a computation.
type Result<'TSuccess, 'TMessage> = 
    /// Represents the result of a successful computation.
    | OK of 'TSuccess * 'TMessage list
    /// Represents the result of a failed computation.
    | Fail of 'TMessage list

    /// Creates a Failure result with the given messages.
    static member FailWith(messages:'TMessage seq) : Result<'TSuccess, 'TMessage> = Result<'TSuccess, 'TMessage>.Fail(messages |> Seq.toList)

    /// Creates a Failure result with the given message.
    static member FailWith(message:'TMessage) : Result<'TSuccess, 'TMessage> = Result<'TSuccess, 'TMessage>.Fail([message])
    
    /// Creates a Success result with the given value.
    static member Succeed(value:'TSuccess) : Result<'TSuccess, 'TMessage> = Result<'TSuccess, 'TMessage>.OK(value,[])

    /// Creates a Success result with the given value and the given message.
    static member Succeed(value:'TSuccess,message:'TMessage) : Result<'TSuccess, 'TMessage> = Result<'TSuccess, 'TMessage>.OK(value,[message])

    /// Creates a Success result with the given value and the given message.
    static member Succeed(value:'TSuccess,messages:'TMessage seq) : Result<'TSuccess, 'TMessage> = Result<'TSuccess, 'TMessage>.OK(value,messages |> Seq.toList)

    /// Converts the result into a string.
    override this.ToString() =
        match this with
        | OK(v,msgs) -> sprintf "OK: %A - %s" v (String.Join(Environment.NewLine, msgs |> Seq.map (fun x -> x.ToString())))
        | Fail(msgs) -> sprintf "Error: %s" (String.Join(Environment.NewLine, msgs |> Seq.map (fun x -> x.ToString())))    

/// Basic combinators and operators for error handling.
[<AutoOpen>]
module Trial =  
    /// Wraps a value in a Success
    let inline ok<'TSuccess,'TMessage> (x:'TSuccess) : Result<'TSuccess,'TMessage> = OK(x, [])

    /// Wraps a value in a Success
    let inline pass<'TSuccess,'TMessage> (x:'TSuccess) : Result<'TSuccess,'TMessage> = OK(x, [])

    /// Wraps a value in a Success and adds a message
    let inline warn<'TSuccess,'TMessage> (msg:'TMessage) (x:'TSuccess) : Result<'TSuccess,'TMessage> = OK(x,[msg])

    /// Wraps a message in a Failure
    let inline fail<'TSuccess,'Message> (msg:'Message) : Result<'TSuccess,'Message> = Fail([ msg ])

    /// Returns true if the result was not successful.
    let inline failed result = 
        match result with
        | Fail _ -> true
        | _ -> false

    /// Takes a Result and maps it with fSuccess if it is a Success otherwise it maps it with fFailure.
    let inline either fSuccess fFailure trialResult = 
        match trialResult with
        | OK(x, msgs) -> fSuccess (x, msgs)
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
        let inline fSuccess (x, msgs2) = OK(x, msgs @ msgs2)
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
        | OK(f, msgs1), OK(x, msgs2) -> OK(f x, msgs1 @ msgs2)
        | Fail errs, OK(_, msgs) -> Fail(errs)
        | OK(_, msgs), Fail errs -> Fail(errs)
        | Fail errs1, Fail errs2 -> Fail(errs1)

    /// If the wrapped function is a success and the given result is a success the function is applied on the value. 
    /// Otherwise the exisiting error messages are propagated.
    /// This is the infix operator version of ErrorHandling.apply
    let inline (<*>) wrappedFunction result = apply wrappedFunction result

    /// Lifts a function into a Result container and applies it on the given result.
    let inline lift f result = apply (ok f) result

    /// Lifts a function into a Result and applies it on the given result.
    /// This is the infix operator version of ErrorHandling.lift
    let inline (<!>) f result = lift f result

    /// Promote a function to a monad/applicative, scanning the monadic/applicative arguments from left to right.
    let inline lift2 f a b = f <!> a <*> b

    /// If the result is a Success it executes the given success function on the value and the messages.
    /// If the result is a Failure it executes the given failure function on the messages.
    /// Result is propagated unchanged.
    let inline eitherTee fSuccess fFailure result =
        let inline tee f x = f x; x;
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
            | OK(rs, m1), OK(r, m2) -> OK(r :: rs, m1 @ m2)
            | OK(_, m1), Fail(m2) | Fail(m1), OK(_, m2) -> Fail(m1 @ m2)
            | Fail(m1), Fail(m2) -> Fail(m1 @ m2)) (ok []) xs
        |> lift List.rev

    /// Converts an option into a Result.
    let inline failIfNone message result = 
        match result with
        | Some x -> ok x
        | None -> fail message

    /// Categorizes a result based on its state and the presence of extra messages
    let inline (|Pass|Warn|Fail|) result =
      match result with
      | OK  (value, []  ) -> Pass  value
      | OK  (value, msgs) -> Warn (value,msgs)
      | Fail        msgs  -> Fail        msgs

    let inline failOnWarnings result =
      match result with
      | Warn (_,msgs) -> Fail msgs
      | _             -> result 

    /// Builder type for error handling computation expressions.
    type TrialBuilder() = 
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
    let trial = TrialBuilder()

/// Represents the result of an async computation
type AsyncResult<'a, 'b> = 
    | AR of Async<Result<'a, 'b>>

/// Useful functions for combining error handling computations with async computations.
[<AutoOpen>]
module AsyncExtensions = 
    /// Useful functions for combining error handling computations with async computations.
    [<RequireQualifiedAccess>]
    module Async = 
        /// Creates an async computation that return the given value
        let singleton value = value |> async.Return

        /// Creates an async computation that runs a computation and
        /// when it generates a result run a binding function on the said result
        let bind f x = async.Bind(x, f)

        /// Creates an async computation that runs a mapping function on the result of an async computation
        let map f x = x |> bind (f >> singleton)

        /// Creates an async computation from an asyncTrial computation
        let ofAsyncResult (AR x) = x

/// Basic support for async error handling computation
[<AutoOpen>]
module AsyncTrial = 
    /// Builder type for error handling in async computation expressions.
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
    
    // Wraps async computations in an error handling computation expression.
    let asyncTrial = AsyncTrialBuilder()

namespace Chessie.ErrorHandling.CSharp

open System
open System.Runtime.CompilerServices
open Chessie.ErrorHandling

/// Extensions methods for easier C# usage.
[<Extension>]
type ResultExtensions () =
    /// Allows pattern matching on Results from C#.
    [<Extension>]
    static member inline Match(value, ifSuccess:Action<'TSuccess , ('TMessage list)>, ifFailure:Action<'TMessage list>) =
        match value with
        | Result.OK(x, msgs) -> ifSuccess.Invoke(x,msgs)
        | Result.Fail(msgs) -> ifFailure.Invoke(msgs)
    
    /// Allows pattern matching on Results from C#.
    [<Extension>]
    static member inline Either(value, ifSuccess:Func<'TSuccess , ('TMessage list),'TResult>, ifFailure:Func<'TMessage list,'TResult>) =
        match value with
        | Result.OK(x, msgs) -> ifSuccess.Invoke(x,msgs)
        | Result.Fail(msgs) -> ifFailure.Invoke(msgs)

    /// Lifts a Func into a Result and applies it on the given result.
    [<Extension>]
    static member inline Map(value,func:Func<_,_>) =
        lift func.Invoke value

    /// Collects a sequence of Results and accumulates their values.
    /// If the sequence contains an error the error will be propagated.
    [<Extension>]
    static member inline Collect(values) =
        collect values

    /// Collects a sequence of Results and accumulates their values.
    /// If the sequence contains an error the error will be propagated.
    [<Extension>]
    static member inline Flatten(value) : Result<seq<'a>,'b>=
        match value with
        | Result.OK(values:Result<'a,'b> seq, msgs:'b list) -> 
            match collect values with
            | Result.OK(values,msgs) -> OK(values |> List.toSeq,msgs)
            | Result.Fail(msgs:'b list) -> Fail msgs
        | Result.Fail(msgs:'b list) -> Fail msgs

    /// If the result is a Success it executes the given Func on the value.
    /// Otherwise the exisiting failure is propagated.
    [<Extension>]
    static member inline SelectMany (o, f: Func<_,_>) =
        bind f.Invoke o

    /// If the result is a Success it executes the given Func on the value.
    /// If the result of the Func is a Succes it maps it using the given Func.
    /// Otherwise the exisiting failure is propagated.
    [<Extension>]
    static member SelectMany (o, f: Func<_,_>, mapper: Func<_,_,_>) =
        let mapper = lift2 (fun a b -> mapper.Invoke(a,b))
        let v = bind f.Invoke o
        mapper o v

    /// Lifts a Func into a Result and applies it on the given result.
    [<Extension>]
    static member Select (o, f: Func<_,_>) = lift f.Invoke o

    /// Returns the error messages or fails if the result was a success.
    [<Extension>]
    static member FailedWith(this:Result<'TSuccess, 'TMessage>) = 
        match this with
        | Result.OK(v,msgs) -> failwithf "Result was a success: %A - %s" v (String.Join(Environment.NewLine, msgs |> Seq.map (fun x -> x.ToString())))
        | Result.Fail(msgs) -> msgs

    /// Returns the result or fails if the result was an error.
    [<Extension>]
    static member SucceededWith(this:Result<'TSuccess, 'TMessage>) : 'TSuccess = 
        match this with
        | Result.OK(v,msgs) -> v
        | Result.Fail(msgs) -> failwithf "Result was an error: %s" (String.Join(Environment.NewLine, msgs |> Seq.map (fun x -> x.ToString())))
