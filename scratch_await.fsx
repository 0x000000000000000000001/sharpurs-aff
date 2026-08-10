open System
open System.Threading
open System.Threading.Tasks

let test () = async {
    let t = Task.Run(fun () -> Thread.Sleep(2000); printfn "Task finished"; 42)
    try
        let! res = Async.AwaitTask t
        printfn "Got %d" res
    with
    | e -> printfn "Caught %A" e
}

let cts = new CancellationTokenSource()
Async.Start(test (), cts.Token)

Thread.Sleep(500)
cts.Cancel()

Thread.Sleep(3000)
