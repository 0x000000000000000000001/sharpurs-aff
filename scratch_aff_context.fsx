open System
open System.Threading

type AffContext = {
    Token: CancellationToken
    mutable KillError: Exception option
}

let ctx = { Token = CancellationToken.None; KillError = None }
printfn "Done"
