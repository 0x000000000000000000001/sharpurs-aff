module Performance_Minibench_FFI

open System
open System.Diagnostics

let toFixed = fun (n: obj) ->
    let num = unbox<float> n
    box (Math.Round(num, 2).ToString("0.00"))

let gc = box (fun _ ->
    GC.Collect()
    box ()
)

let timeNs = fun (k: obj) ->
    let sw = Stopwatch.StartNew()
    sharpurs_apply k undefined |> ignore
    sw.Stop()
    let ns = float sw.ElapsedTicks * (1000000000.0 / float Stopwatch.Frequency)
    box ns
