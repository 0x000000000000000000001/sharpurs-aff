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
        printfn "Start acquire"
        do! Async.Sleep(2000)
        printfn "Finish acquire"
        return 42
    }, cancellationToken = CancellationToken.None)
    
    try
        let! res = awaitUninterruptibly t
        printfn "Got %d" res
    with
    | e -> printfn "Caught %A" e
}

let cts = new CancellationTokenSource()
Async.Start(test (), cts.Token)

Thread.Sleep(500)
cts.Cancel()
printfn "Cancelled main token"

Thread.Sleep(3000)
