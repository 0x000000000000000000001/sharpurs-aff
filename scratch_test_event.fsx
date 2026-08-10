open System
open System.Threading

let ev = new Event<unit>()
let mutable result = None

let tcs = new System.Threading.Tasks.TaskCompletionSource<obj>()

printfn "Started"
