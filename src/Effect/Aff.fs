module Effect.Aff

open System
open System.Threading

let rec _unwrapException (e: Exception) =
    match e with
    | :? AggregateException as ae when ae.InnerExceptions.Count = 1 -> _unwrapException ae.InnerException
    | _ -> e

type AffState = { Token: CancellationToken; KillError: Exception option ref; Supervisor: CancellationTokenSource option }
type AffFn = AffState -> Async<obj>

type NativeFiber(aff: AffFn) =
    let tcs = new System.Threading.Tasks.TaskCompletionSource<Result<obj, Exception>>()
    let cts = new CancellationTokenSource()
    let killError = ref None
    let mutable started = false
    
    member this.Cts = cts
    member this.Aff = aff
    member this.KillError = killError.Value
    member this.Cancel(ex: Exception) =
        killError.Value <- Some ex
        cts.Cancel()
    member this.Start() =
        lock this (fun () ->
            if not started then
                started <- true
                Sharpurs_Prelude.SharpursRuntime.EventLoopAdd(1)
                Async.StartWithContinuations(
                    aff { Token = cts.Token; KillError = killError; Supervisor = None },
                    (fun res -> 
                        tcs.TrySetResult(Ok res) |> ignore
                        Sharpurs_Prelude.SharpursRuntime.EventLoopDone()
                    ),
                    (fun ex -> 
                        tcs.TrySetResult(Error (_unwrapException ex)) |> ignore
                        Sharpurs_Prelude.SharpursRuntime.EventLoopDone()
                    ),
                    (fun ex -> 
                        tcs.TrySetResult(Error (new Exception("Cancelled", _unwrapException ex))) |> ignore
                        Sharpurs_Prelude.SharpursRuntime.EventLoopDone()
                    ),
                    CancellationToken.None
                )
        )
        
    member this.WaitAsync() = Async.AwaitTask tcs.Task
    member this.IsSuspended = not started

let _applyFn (func: obj) (arg: obj) : obj =
    let method = func.GetType().GetMethods() |> Array.find (fun m -> m.Name = "Invoke" && m.GetParameters().Length = 1)
    method.Invoke(func, [| arg |])

let _pure = fun (a: obj) -> 
    box (fun (ctx: AffState) -> async.Return(a))

let _throwError = fun (eVal: obj) -> 
    box (fun (ctx: AffState) -> async {
        let e = eVal :?> Exception
        return raise e
    })

let _catchError = fun (affVal: obj) -> fun (kVal: obj) ->
    box (fun (ctx: AffState) -> async {
        try
            let aff = affVal :?> AffFn
            return! aff ctx
        with 
        | :? Exception as e ->
            let handler = _applyFn kVal (box e) |> unbox<AffFn>
            return! handler ctx
    })


let _map = fun (fVal: obj) -> fun (affVal: obj) ->
    box (fun (ctx: AffState) -> async {
        let aff = affVal :?> AffFn
        let! res = aff ctx
        return _applyFn fVal res
    })

let _bind = fun (affVal: obj) -> fun (kVal: obj) ->
    box (fun (ctx: AffState) -> async {
        if ctx.Token.IsCancellationRequested then
            return raise (defaultArg ctx.KillError.Value (new Exception("Cancelled")))
        else
            let aff = affVal :?> AffFn
            let! res = aff ctx
            if ctx.Token.IsCancellationRequested then
                return raise (defaultArg ctx.KillError.Value (new Exception("Cancelled")))
            else
                let nextAff = _applyFn kVal res |> unbox<AffFn>
                return! nextAff ctx
    })

let _delay = fun (rightVal: obj) -> fun (msVal: obj) ->
    box (fun (ctx: AffState) -> async {
        let ms = msVal :?> float
        try
            let! res = Async.AwaitTask(System.Threading.Tasks.Task.Delay(int ms, ctx.Token))
            return box null
        with
        | :? System.Threading.Tasks.TaskCanceledException ->
            return raise (defaultArg ctx.KillError.Value (new Exception("Cancelled")))
    })

let _liftEffect = fun (effVal: obj) ->
    box (fun (ctx: AffState) -> async {
        return _applyFn effVal null
    })

let _makeFiberNative = fun (affVal: obj) ->
    box (fun (_dummy: obj) ->
        let aff = affVal :?> AffFn
        box (new NativeFiber(aff))
    )

let _runFiber = fun (nfVal: obj) ->
    box (fun (_dummy: obj) ->
        let nf = nfVal :?> NativeFiber
        nf.Start()
        box null
    )

let _killFiber = fun (nfVal: obj) -> fun (errVal: obj) -> fun (onErrorVal: obj) -> fun (onSuccessVal: obj) ->
    box (fun (_dummy: obj) ->
        let nf = nfVal :?> NativeFiber
        let ex = errVal :?> Exception
        nf.Cancel(ex)
        nf.Start() // In case it wasn't started
        
        Async.Start(async {
            let! res = nf.WaitAsync()
            let effect = _applyFn onSuccessVal (box null)
            _applyFn effect null |> ignore
        })

        box (fun (_dummy: obj) -> box null)
    )

let _joinFiber = fun (nfVal: obj) -> fun (onErrorVal: obj) -> fun (onSuccessVal: obj) ->
    box (fun (_dummy: obj) ->
        let nf = nfVal :?> NativeFiber
        nf.Start() // In case it wasn't started
        
        Async.Start(async {
            let! res = nf.WaitAsync()
            match res with
            | Ok v ->
                let effect = _applyFn onSuccessVal v
                _applyFn effect null |> ignore
            | Error ex ->
                let effect = _applyFn onErrorVal (box ex)
                _applyFn effect null |> ignore
        })

        box (fun (_dummy: obj) -> box null)
    )

let _onCompleteFiber = fun (nfVal: obj) -> fun (onCompleteVal: obj) ->
    box (fun (_dummy: obj) ->
        box (fun (_dummy: obj) -> box null) // Stub
    )

let _isSuspendedFiber = fun (nfVal: obj) ->
    box (fun (_dummy: obj) ->
        let nf = nfVal :?> NativeFiber
        box nf.IsSuspended
    )

let _makeAffImpl = fun (buildVal: obj) ->
    box (fun (ctx: AffState) -> async {
        let tcs = new System.Threading.Tasks.TaskCompletionSource<obj>()
        let mutable completed = false
        let lockObj = obj()
        
        let successCb (res: obj) = 
            let lockTaken = lock lockObj (fun () -> if completed then false else completed <- true; true)
            if lockTaken then tcs.TrySetResult(res) |> ignore
            box (fun (_: obj) -> box null)

        let errorCb (errVal: obj) =
            let lockTaken = lock lockObj (fun () -> if completed then false else completed <- true; true)
            let err = errVal :?> Exception
            if lockTaken then tcs.TrySetException(err) |> ignore
            box (fun (_: obj) -> box null)

        let buildFn = _applyFn buildVal (box errorCb)
        let effCanceler = _applyFn buildFn (box successCb)
        let canceler = _applyFn effCanceler null
        
        let reg = ctx.Token.Register(fun () ->
            let actualEx = defaultArg ctx.KillError.Value (new Exception("Cancelled"))
            let cancelAffObj = _applyFn canceler (box actualEx)
            printfn "cancelAffObj type: %s" (cancelAffObj.GetType().FullName)
            let cancelAff = cancelAffObj |> unbox<AffFn>
            let emptyCtx = { Token = CancellationToken.None; KillError = ref None; Supervisor = None }
            let t = Async.StartAsTask(cancelAff emptyCtx, cancellationToken = CancellationToken.None)
            t.ContinueWith(fun (t2: System.Threading.Tasks.Task<obj>) -> 
                let lockTaken = lock lockObj (fun () -> if completed then false else completed <- true; true)
                if lockTaken then tcs.TrySetException(actualEx) |> ignore
            ) |> ignore
        )
        
        try
            try
                let! res = Async.AwaitTask(tcs.Task)
                return res
            with
            | ex ->
                return raise (_unwrapException ex)
        finally
            reg.Dispose()
    })

let _forkAffNative = fun (affVal: obj) ->
    box (fun (ctx: AffState) -> async {
        let aff = affVal :?> AffFn
        let nf = new NativeFiber(aff)
        match ctx.Supervisor with
        | Some supCts -> supCts.Token.Register(fun () -> nf.Cts.Cancel()) |> ignore
        | None -> ()
        return box nf
    })

let _makeSupervisedFiber = fun (affVal: obj) ->
    let f : obj -> obj = fun _dummy ->
        let aff = affVal :?> AffFn
        let supCts = new CancellationTokenSource()
        let supervisedAff (ctx: AffState) = async {
            return! aff { ctx with Supervisor = Some supCts }
        }
        let nf = new NativeFiber(supervisedAff)
        
        supCts.Token.Register(fun () -> nf.Cts.Cancel()) |> ignore
        
        let recd = Map.empty |> Map.add "supervisor" (box supCts) |> Map.add "fiber" (box nf)
        box recd
    box f

let _killAll = fun (errVal: obj) -> fun (supVal: obj) -> fun (cbVal: obj) ->
    let f : obj -> obj = fun _dummy ->
        let supCts = supVal :?> CancellationTokenSource
        supCts.Cancel()
        _applyFn cbVal null |> ignore
        let canceler : obj -> obj = fun _dummy2 -> box null
        box canceler
    box f

let _sequential = fun (affVal: obj) -> affVal

let awaitUninterruptibly (t: System.Threading.Tasks.Task<'T>) =
    Async.FromContinuations(fun (onSuccess, onError, _) ->
        let mutable completed = false
        let lockObj = obj()
        t.ContinueWith(fun (t2: System.Threading.Tasks.Task<'T>) ->
            let lockTaken = lock lockObj (fun () -> 
                if completed then false else completed <- true; true)
            if lockTaken then
                if t2.IsFaulted then onError (_unwrapException t2.Exception)
                elif t2.IsCanceled then onError (new OperationCanceledException())
                else onSuccess t2.Result
        ) |> ignore
    )

let generalBracket = fun (acquireVal: obj) -> fun (optionsVal: obj) -> fun (useVal: obj) ->
    box (fun (ctx: AffState) -> async {
        let acquireFn = acquireVal :?> AffFn
        // acquire is uninterruptible
        let emptyCtx = { Token = CancellationToken.None; KillError = ref None; Supervisor = None }
        let! resourceResult = async {
            try
                let t = Async.StartAsTask(acquireFn emptyCtx, cancellationToken = CancellationToken.None)
                let! res = awaitUninterruptibly t
                return Ok res
            with
            | :? Exception as e -> 
                return Error e
        }
        
        match resourceResult with
        | Error err -> return raise err
        | Ok resource ->
            let options = optionsVal :?> Map<string, obj>
            
            // Check if killed during acquire
            if ctx.Token.IsCancellationRequested then
                let ex = defaultArg ctx.KillError.Value (new Exception("Cancelled"))
                let cleanupFn = _applyFn (_applyFn options.["killed"] (box ex)) resource |> unbox<AffFn>
                let t = Async.StartAsTask(cleanupFn emptyCtx, cancellationToken = CancellationToken.None)
                let! _ = awaitUninterruptibly t
                return raise ex
            else
                let useFn = _applyFn useVal resource |> unbox<AffFn>
                let! useResult = async {
                    try 
                        let! res = useFn ctx
                        return Ok res
                    with 
                    | :? Exception as e -> return Error e
                }
                
                let cleanupAffFn = 
                    match useResult with
                    | Ok v ->
                        let cleanupFn = _applyFn (_applyFn options.["completed"] v) resource |> unbox<AffFn>
                        cleanupFn
                    | Error e ->
                        if e.Message = "Cancelled" || e :? OperationCanceledException then
                            let actualEx = defaultArg ctx.KillError.Value e
                            let cleanupFn = _applyFn (_applyFn options.["killed"] (box actualEx)) resource |> unbox<AffFn>
                            cleanupFn
                        else
                            let cleanupFn = _applyFn (_applyFn options.["failed"] (box e)) resource |> unbox<AffFn>
                            cleanupFn
                        
                let t = Async.StartAsTask(cleanupAffFn emptyCtx, cancellationToken = CancellationToken.None)
                let! _ = awaitUninterruptibly t
                
                match useResult with
                | Ok v -> return v
                | Error e -> return raise e
    })

let _parAffMap = fun (fVal: obj) -> fun (affVal: obj) ->
    box (fun (ctx: AffState) -> async {
        let aff = affVal :?> AffFn
        let! res = aff ctx
        return _applyFn fVal res
    })

let _parAffApply = fun (aff1Val: obj) -> fun (aff2Val: obj) ->
    box (fun (ctx: AffState) -> async {
        let aff1 = aff1Val :?> AffFn
        let aff2 = aff2Val :?> AffFn
        
        let! child1 = Async.StartChild(aff1 ctx)
        let! child2 = Async.StartChild(aff2 ctx)
        let! res1 = child1
        let! res2 = child2
        
        return _applyFn res1 res2
    })

let _parAffAlt = fun (aff1Val: obj) -> fun (aff2Val: obj) ->
    box (fun (ctx: AffState) -> async {
        let cts = CancellationTokenSource.CreateLinkedTokenSource(ctx.Token)
        let token = { Token = cts.Token; KillError = ctx.KillError; Supervisor = None }
        let aff1 = aff1Val :?> AffFn
        let aff2 = aff2Val :?> AffFn
        
        let t1 = Async.StartAsTask(aff1 token, cancellationToken = CancellationToken.None)
        let t2 = Async.StartAsTask(aff2 token, cancellationToken = CancellationToken.None)
        
        let! firstCompleted = System.Threading.Tasks.Task.WhenAny(t1, t2) |> Async.AwaitTask
        let otherTask = if firstCompleted = t1 then t2 else t1
        
        if firstCompleted.Status = System.Threading.Tasks.TaskStatus.RanToCompletion then
            if not otherTask.IsCompleted then cts.Cancel()
            try
                let! _ = Async.AwaitTask(otherTask)
                ()
            with | _ -> ()
            return firstCompleted.Result
        else
            try
                let! res = Async.AwaitTask(otherTask)
                return res
            with
            | _ -> 
                return raise (_unwrapException firstCompleted.Exception)
    })
