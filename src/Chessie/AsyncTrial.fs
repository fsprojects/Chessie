namespace Chessie.ErrorHandling

type AsyncResult<'a,'b> = AR of Async<Result<'a, 'b>>

[<AutoOpen>]
module AsyncExtensions =
    
    [<RequireQualifiedAccess>]
    module Async =

            let singleton value = value |> async.Return

            let bind f x = async.Bind(x,f)

            let map f x = x |> bind (f >> singleton)

            let ofAsyncResult (AR x) = x

[<AutoOpen>]
module AsyncTrial =
        
    type AsyncTrialBuilder () =
    
        member __.Return value : AsyncResult<'a, 'b> =
            value |> ok |> Async.singleton |> AR

        member __.ReturnFrom (asyncResult : AsyncResult<'a, 'b>) =
            asyncResult

        member this.Zero () :  AsyncResult<unit, 'b> =
            this.Return ()

        member __.Delay (generator : unit -> AsyncResult<'a, 'b>) : AsyncResult<'a, 'b> =
            async.Delay (generator >> Async.ofAsyncResult) |> AR

        member __.Bind (asyncResult : AsyncResult<'a, 'c>, binder : 'a -> AsyncResult<'b,'c>) : AsyncResult<'b, 'c> =
            let fSuccess (value, msgs) = value |> (binder >> Async.ofAsyncResult >> Async.map (mergeMessages msgs))
            let fFailure errs = errs |> Fail |> Async.singleton

            asyncResult |> Async.ofAsyncResult |> Async.bind (either fSuccess fFailure) |> AR

        member this.Bind (result : Result<'a, 'c>, binder : 'a -> AsyncResult<'b,'c>) : AsyncResult<'b, 'c> =
            this.Bind(result |> Async.singleton |> AR, binder)

        member __.Bind (async : Async<'a>, binder : 'a -> AsyncResult<'b,'c>) : AsyncResult<'b, 'c> =
            async |> Async.bind (binder >> Async.ofAsyncResult) |> AR

        member __.TryWith (asyncResult : AsyncResult<'a, 'b>, catchHandler : exn -> AsyncResult<'a, 'b>)
            : AsyncResult<'a, 'b> =
            async.TryWith(asyncResult |> Async.ofAsyncResult, (catchHandler >> Async.ofAsyncResult)) |> AR

        member __.TryFinally (asyncResult : AsyncResult<'a, 'b>, compensation : unit -> unit)
           : AsyncResult<'a, 'b> =
            async.TryFinally (asyncResult |> Async.ofAsyncResult, compensation) |> AR

        member __.Using (resource : ('T :> System.IDisposable), binder : 'T -> AsyncResult<'a,'b>)
            : AsyncResult<'a, 'b> = 
            async.Using (resource, (binder >> Async.ofAsyncResult)) |> AR

    let asyncTrial = AsyncTrialBuilder()
    

    
     