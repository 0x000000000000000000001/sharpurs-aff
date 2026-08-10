module Test_QuickCheck_Gen_FFI

open System

let float32ToInt32 = fun (n: obj) ->
    let num = unbox<float> n
    let f32 = float32 num
    let bytes = BitConverter.GetBytes(f32)
    box (BitConverter.ToInt32(bytes, 0))
