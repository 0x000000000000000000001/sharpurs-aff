open System
open System.Threading
open System.Threading.Tasks

let test (ct: CancellationToken) = async {
    try
        printfn "Start delay"
        let! res = Async.AwaitTask(Task.Delay(2000, ct))
        printfn "Finish delay"
    with
    | e -> printfn "Caught %A" (e.GetType())
}

let cts = new CancellationTokenSource()
Async.StartWithContinuations(test cts.Token,
    (fun _ -> printfn "Success"),
    (fun ex -> printfn "Error: %A" ex),
    (fun ex -> printfn "Cancelled!"),
    CancellationToken.None) // Ambient is None!

Thread.Sleep(500)
cts.Cancel()

Thread.Sleep(3000)
