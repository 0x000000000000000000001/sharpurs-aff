module Control_Extend_FFI

let arrayExtend = fun (f: obj) -> fun (xs: obj) ->
    let arr = unbox<obj[]> xs
    let res = Array.zeroCreate arr.Length
    for i = 0 to arr.Length - 1 do
        let sub = Array.sub arr i (arr.Length - i)
        res.[i] <- sharpurs_apply f (box sub)
    box res
