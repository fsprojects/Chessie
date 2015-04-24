/// Contains error propagation functions and a computation expression builder for Railway-oriented programming.
namespace Chessie.ErrorHandling

open System

/// Represents the result of a computation
type Outcome<'success,'message> =
  | Pass of success:'success * warning:'message list
  | Fail of failure:'message list


/// Provides an array of functions for working with instances of `Outcome<'success,'message>`
[<AutoOpen>]
module Trial =
  /// Creates a passing outcome from the given value
  let inline pass value = Pass (value,[])

  /// Creates a failing outcome from the given error
  let inline fail error = Fail [error]

  /// Creates a warning outcome from the given message and value
  let inline warn msg value = Pass (value,[msg])

  /// Returns `true` if outcome is in the failed case; returns false otherwise
  let hasFailed outcome =
    match outcome with
    | Fail _  -> true
    | _       -> false
 
  /// Matches (and extracts) any warning messages on an outcome
  let (|Warn|_|) outcome =
    match outcome with
    | Pass (_,[])   -> None
    | Pass (_,log)  -> Some log
    | Fail _        -> None

  /// Returns `true` if the outcome has warning messages; returns false otherwise
  let hasWarnings outcome =
    match outcome with
    | Warn _  -> true
    | _       -> false

  /// Converts a passing outcome with warning messages into a failing outcome
  let failOnWarnings withWarn outcome =
    match outcome with
    | Warn log  -> Fail (withWarn log)
    | _         -> outcome
  
  /// Uses `withPass` to combine the results of executing `next1` and `next2`
  /// against `input`, when both executions return passing outcomes;
  /// Failing outcomes are propagated (and, if necessary, combined)
  let plus withPass next1 next2 input =
    match next1 input, next2 input with
    | Pass (v1,m1), Pass (v2,m2)  -> Pass (withPass v1 v2,m2 @ m1)
    | Fail failure, Pass _
    | Pass _      , Fail failure  -> Fail failure
    | Fail error1 , Fail error2   -> Fail (error2 @ error1)

  /// Returns the first passing outcome (if any)
  let andAlso next1 next2 input =
    let inline withPass value _ = value
    plus withPass next1 next2 input

  /// Returns the second passing outcome (if any)
  let orElse next1 next2 input =
    let inline withPass _ value = value
    plus withPass next1 next2 input

  /// Applies `withPass` to a passing outcome or `withFail` to a failing outcome
  let either withPass withFail outcome =
    match outcome with
    | Pass (v,log) -> withPass (v,log) 
    | Fail failure -> withFail failure

  /// Applies `withPass` to a passing outcome or `withFail` to a failing outcome; 
  /// The original outcome is returned unmodified
  let eitherTee withPass withFail outcome =
    outcome |> either withPass withFail; outcome

  /// Applies `withPass` to a passing outcome; 
  /// The original outcome is returned unmodified
  let passTee withPass outcome = 
    outcome |> eitherTee withPass ignore
   
  /// Applies `withFail` to a failing outcome; 
  /// The original outcome is returned unmodified
  let failTee withFail outcome =
    outcome |> eitherTee ignore withFail

  /// Processes an outcome using either of the two given one-track functions (one for success 
  /// and one for failure) by first converting said functions into two-track functions
  let bimap nextPass nextFail outcome =
    let inline withPass (v,log) = Pass (nextPass v,log)
    let inline withFail failure = Fail (nextFail failure)
    outcome |> either withPass withFail

  /// Converts a one-track function into a two-track function by executing it against an outcome
  let map next outcome = 
    outcome |> bimap next id

  /// Merges messages, either warnings or errors, into the given outcome
  let mergeLog msgs outcome =
    let inline withPass (v,log) = Pass (v,msgs @ log)
    let inline withFail failure = Fail (msgs @ failure)
    outcome |> either withPass withFail

  /// Executes the given function on the outcome if it is a passing outcome; 
  /// Otherwise the failing outcome is propagates as-is
  let bind next outcome =
    let inline withPass (v,log) = next v |> mergeLog log
    let inline withFail failure = Fail failure
    outcome |> either withPass withFail

  /// An alias for `Trial.bind`
  let inline (>>=) outcome next = outcome |> bind next 

  /// Creates a new outcome-producing function be combining two other outcome-producing functions in sequence
  let inline (>=>) next1 next2 = next1 >> (bind next2)

  /// Creates a two-track function from a one-track function
  let lift next = next >> pass

  /// Collects a sequence of outcomes and accumulates their values; 
  /// if the sequence contains an error the error will be propagated
  let collect outcomes =
    let inline withPass (v,log) = Pass (List.rev v,log)
    outcomes
    |> Seq.fold (fun outcome next ->
                  match outcome , next with
                  | Pass (v1,m1), Pass (v2,m2) -> Pass (v2::v1,m2 @ m1)
                  | Fail failure, Pass _
                  | Pass _      , Fail failure -> Fail (failure)
                  | Fail errors1, Fail errors2 -> Fail (errors2 @ errors2))
                (pass [])
    |> either withPass Fail

  /// Flattens a nested result given the Failure types are equal
  let flatten outcome = outcome |> bind id

  /// Executes the given function on a passing outcome or captures the failure
  let tryCatch next cover input =
    try 
      next input |> pass 
    with 
      | x -> cover x |> fail

  /// Returns the value from a passing outcome 
  /// or executes the given function against a failing outcome
  let returnOrHandle withFail outcome =
    let inline withPass (value,_) = value
    outcome |> either withPass withFail

  /// Returns the value from a passing outcome 
  /// or raises an exception with messages from a failing outcome
  let returnOrRaise outcome =
    let inline withFail errors = 
      errors 
      |> Seq.map (sprintf "%O")
      |> String.concat (Environment.NewLine + "\t")
      |> failwith
    outcome |> returnOrHandle withFail

  module Quiet =
    /// Recategories an `Outcome<'success,'message>` as such that warnings are ignored
    let (|Ok|Bad|) outcome =
      match outcome with
      | Pass (v,_)  -> Ok v
      | Fail errors -> Bad errors
   
    /// Applies `withPass` to a passing outcome or `withFail` to a failing outcome
    let eitherQuiet withPass withFail outcome = 
      let inline withOk (v,_) = withPass v
      either withOk withFail outcome

    /// Applies `withPass` to a passing outcome or `withFail` to a failing outcome; 
    /// The original outcome is returned unmodified
    let eitherTeeQuiet withPass withFail outcome =
      outcome |> eitherQuiet withPass withFail; outcome

    /// Applies `withPass` to a passing outcome; 
    /// The original outcome is returned unmodified
    let passTeeQuiet withPass outcome = 
      outcome |> eitherTeeQuiet withPass ignore

    /// Applies `withFail` to a failing outcome; 
    /// The original outcome is returned unmodified
    let failTeeQuiet withFail outcome =
      outcome |> eitherTeeQuiet ignore withFail


/// Contains computation expressions used in rail-way oriented error handling and validation
[<AutoOpen>]
module Control =
  /// Provides the `trial` computation expression
  type TrialBuilder() = 
    /// A new (empty) expression
    member __.Zero () = pass ()
    /// Sequences two expressions
    member __.Bind (trial,next) = bind next trial
    /// Brings a value into the expression
    member __.Return value = pass value
    /// Flattens a nested expression
    member __.ReturnFrom trial = trial
    /// Sequences two expressions
    member __.Combine (trial,next) = bind next trial
    /// Stages an expression for eventual execution
    member __.Delay next : (unit -> 'next) = next
    /// Executes an expression
    member __.Run trial = trial ()
    /// Ensures a compensation is run if an expression raises an exception
    member __.TryWith (body,cover) = try body () with x -> cover x
    /// Ensures a compensation is run if an expression raises an exception
    member __.TryFinally (body,cleanup) = try body () finally cleanup ()
    /// Ensures an expression cleans up disposable resources
    member B.Using (resource:#IDisposable,body) =
      B.TryFinally (fun () -> body resource
                   ,fun () -> if resource <> null then resource.Dispose ())
    /// Loops an expresion until some boolean condition is met
    member B.While (guard,body) =
      if not <| guard () 
        then B.Zero ()
        else bind (fun () -> B.While (guard,body)) (body())
    /// Iterates a sequence of expressions
    member B.For (items:seq<_>,body) =
      B.Using (items.GetEnumerator()
              ,fun enum ->  B.While(enum.MoveNext
                                   ,B.Delay(fun () -> body enum.Current)))
  
  /// Single instance of the `trial` computation expression builder
  let trial = TrialBuilder ()


[<AutoOpen>]
module AsyncTrial =
  /// Represents the result of an async computation
  type AsyncResult<'a, 'b> = 
  | AR of Async<Outcome<'a, 'b>>

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

  /// Builder type for error handling in async computation expressions.
  type AsyncTrialBuilder() = 
      member __.Return value : AsyncResult<'a, 'b> = 
        value
        |> pass
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
                      >> Async.map (mergeLog msgs))
        
        let fFailure errs = 
            errs
            |> Fail
            |> Async.singleton
        
        asyncResult
        |> Async.ofAsyncResult
        |> Async.bind (either fSuccess fFailure)
        |> AR
      
      member this.Bind(result : Outcome<'a, 'c>, binder : 'a -> AsyncResult<'b, 'c>) : AsyncResult<'b, 'c> = 
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


namespace Chessie.ErrorHandling.Compat

open System
open System.Runtime.CompilerServices
open Chessie.ErrorHandling

/// Simplifies creating `Outcome<'Success,'Message>` instances from languages other than F#
type Outcome =
  /// Creates a passing outcome from the given value
  static member PassWith<'Success,'Message> value : Outcome<'Success,'Message> = Pass (value,[])

  /// Creates a failing outcome from the given error
  static member FailWith<'Success,'Message> error : Outcome<'Success,'Message> = Fail [error]

  /// Creates a warning outcome from the given message and value
  static member WarnWith<'Success,'Message> msg value : Outcome<'Success,'Message> = Pass (value,[msg])

  /// Executes the given function on a passing outcome or captures the failure
  static member Try(func: Func<'Success>) : Outcome<'Success,exn> =        
    try
      pass <| func.Invoke()
    with
      | exn -> fail exn


/// Extensions methods for easier use from languages other than F#
[<Extension>]
type OutcomeExtensions =
  /// Executes the appropriate `Action` based on the state of an outcome
  [<Extension>]
  static member Match (this     :Outcome<'Success,'Message>
                      ,withPass :Action<'Success,'Message seq>
                      ,withFail :Action<'Message seq>) =
    match this with
    | Pass (v,log) -> withPass.Invoke (v,log)
    | Fail failure -> withFail.Invoke failure

  /// Executes the appropriate `Func` based on the state of an outcome
  [<Extension>]
  static member Either (this      :Outcome<'Success,'Message>
                       ,withPass  :Func<'Success,'Message seq,'Result>
                       ,withFail  :Func<'Message seq,'Result>) =
    match this with
    | Pass (v,log) -> withPass.Invoke (v,log)
    | Fail failure -> withFail.Invoke failure

  /// Collects a sequence of outcomes and accumulates their values; 
  /// If the sequence contains an error, the error will be propagated
  [<Extension>]
  static member Collect (values :Outcome<'Success,'Message> seq) = collect values

  /// Collects a sequence of outcomes and accumulates their values; 
  /// If the sequence contains an error, the error will be propagated
  [<Extension>]
  static member Flatten this =
    match this with
    | Pass (v :_ seq,log) ->  match collect v with
                              | Pass (v,msg) -> Pass (Seq.ofList v,log @ msg)
                              | Fail failure -> Fail failure
    | Fail failure        ->  Fail failure

  /// Executes `withPass` on a passing outcome;
  /// existing failures are propagated
  [<Extension>]
  static member SelectMany  (this     :Outcome<'Success,'Message>
                            ,withPass :Func<'Success,Outcome<'T,'Message>>) = 
    this |> bind withPass.Invoke

  /// Executes `withPass` on a passing outcome and if that execution is also
  /// passing, applies the given `selector` to the result; 
  /// existing failures are propagated
  [<Extension>]
  static member SelectMany  (this     :Outcome<'Success,'Message>
                            ,withPass :Func<_,_>
                            ,selector :Func<_,_,_>) =
    //TODO: this method is almost certainly wrong!
    match this with
    | Pass (v1,_)   ->  match withPass.Invoke v1 with
                        | Pass (v2,_)   ->  selector.Invoke (v1,v2)
                        | Fail failure  ->  Fail failure
    | Fail failure  ->                      Fail failure

  /// Lifts a `Func` into an outcome and applies it on the given result
  [<Extension>]
  static member Select  (this     :Outcome<'Success,'Message>
                        ,withPass :Func<'Success,'Result>) = 
    this |> bind (lift withPass.Invoke)

  /// Returns the failing messages or raises an exception if the result was a success
  [<Extension>]
  static member FailedWith(this :Outcome<'Success, 'Message>) = 
    match this with
    | Pass (v,log) -> log 
                      |> Seq.map (sprintf "%O")
                      |> String.concat (Environment.NewLine + "\t")
                      |> failwithf "Result was a success: %A - %s" v
    | Fail failure -> failure

  /// Returns the outcome value or raises an exception if the result was an error
  [<Extension>]
  static member SucceededWith(this :Outcome<'Success, 'Message>) : 'Success = 
    match this with
    | Pass (v,_)    ->  v
    | Fail failure  ->  failure 
                        |> Seq.map (sprintf "%O")
                        |> String.concat (Environment.NewLine + "\t")
                        |> failwithf "Result was an error: %s"
  
[<assembly:Extension>]
do ()
