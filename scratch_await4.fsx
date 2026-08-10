open System
open System.Threading
open System.Threading.Tasks

let awaitUninterruptibly (t: Task<'T>) =
    Async.FromContinuations(fun (onSuccess, onError, _) ->
        t.ContinueWith(fun (t2: Task<'T>) ->
            if t2.IsFaulted then onError t2.Exception.InnerException
            elif t2.IsCanceled then onError (new OperationCanceledException())
            else onSuccess t2.Result
        ) |> ignore
    )

let test () = async {
    let t = Async.StartAsTask(async {
        do! Async.Sleep(2000)
        return 42
    }, cancellationToken = CancellationToken.None)
    
    try
        let! res = awaitUninterruptibly t
        printfn "After await! res = %d" res
        do! Async.Sleep(100) // Does this throw?
        printfn "After sleep 100!"
    with
    | e -> printfn "Caught %A" e
}

let cts = new CancellationTokenSource()
Async.StartWithContinuations(test (),
    (fun _ -> printfn "Success"),
    (fun ex -> printfn "Error: %A" ex),
    (fun ex -> printfn "Cancelled!"),
    cts.Token)

Thread.Sleep(500)
cts.Cancel()

Thread.Sleep(3000)
