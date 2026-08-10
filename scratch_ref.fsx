let killError = ref None
killError.Value <- Some (new System.Exception("x"))
printfn "%A" killError.Value
