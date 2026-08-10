open System
open System.Threading.Tasks

let test () = async {
    let tcs = TaskCompletionSource<obj>()
    tcs.TrySetException(Exception("Nope")) |> ignore
    
    try
        let! res = Async.AwaitTask(tcs.Task)
        printfn "Success"
    with
    | ex -> 
        printfn "Caught: %s -> %s" (ex.GetType().Name) ex.Message

}

Async.RunSynchronously(test())
