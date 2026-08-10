import re

with open('src/Effect/Aff.fs', 'r') as f:
    content = f.read()

# Replace assignments and usages:
# let k = kVal :?> (obj -> obj)
# let! affB = k res
# -> let! affB = _applyFn kVal res |> unbox<AffFn>

replacements = [
    (r"let k = kVal :\?> \(obj -> obj\)\s+let! affB = k res", r"let! affB = _applyFn kVal res |> unbox<AffFn>"),
    (r"let f = fVal :\?> \(obj -> obj\)\s+return f res", r"return _applyFn fVal res"),
    (r"let k = kVal :\?> \(obj -> obj\)\s+let aff2 = k res :\?> AffFn", r"let aff2 = _applyFn kVal res |> unbox<AffFn>"),
    (r"let eff = effVal :\?> \(obj -> obj\)\s+return eff null", r"return _applyFn effVal null"),
    (r"let onSuccess = onSuccessVal :\?> \(obj -> obj\)\s+let effect = onSuccess \(box null\) :\?> \(obj -> obj\)\s+effect null \|> ignore", 
     r"let effect = _applyFn onSuccessVal (box null)\n            _applyFn effect null |> ignore"),
    (r"let onSuccess = onSuccessVal :\?> \(obj -> obj\)\s+let effect = onSuccess v :\?> \(obj -> obj\)\s+effect null \|> ignore",
     r"let effect = _applyFn onSuccessVal v\n                _applyFn effect null |> ignore"),
    (r"let onError = onErrorVal :\?> \(obj -> obj\)\s+let effect = onError \(box ex\) :\?> \(obj -> obj\)\s+effect null \|> ignore",
     r"let effect = _applyFn onErrorVal (box ex)\n                _applyFn effect null |> ignore"),
    (r"let effCanceler = build \(box errorCb\) \(box successCb\) :\?> \(obj -> obj\)\s+let canceler = effCanceler null :\?> \(obj -> obj\)",
     r"let effCanceler = _applyFn (_applyFn build (box errorCb)) (box successCb)\n            let canceler = _applyFn effCanceler null"),
    (r"let cb = cbVal :\?> \(obj -> obj\)\s+let effect = cb null :\?> \(obj -> obj\)\s+effect null \|> ignore",
     r"let effect = _applyFn cbVal null\n        _applyFn effect null |> ignore"),
    (r"let useFnMaker = useVal :\?> \(obj -> obj\)\s+let useFn = useFnMaker res :\?> AffFn",
     r"let useFn = _applyFn useVal res |> unbox<AffFn>"),
    (r"let f1 = options\.\[\"completed\"\] :\?> \(obj -> obj\)\s+let f2 = f1 v :\?> \(obj -> obj\)\s+let cleanup = f2 res :\?> AffFn",
     r"let cleanup = _applyFn (_applyFn options.[\"completed\"] v) res |> unbox<AffFn>"),
    (r"let f1 = options\.\[\"failed\"\] :\?> \(obj -> obj\)\s+let f2 = f1 \(box e\) :\?> \(obj -> obj\)\s+let cleanup = f2 res :\?> AffFn",
     r"let cleanup = _applyFn (_applyFn options.[\"failed\"] (box e)) res |> unbox<AffFn>"),
    (r"let f = res1 :\?> \(obj -> obj\)\s+return f res2",
     r"return _applyFn res1 res2"),
]

for pattern, repl in replacements:
    content = re.sub(pattern, repl, content)

with open('src/Effect/Aff.fs', 'w') as f:
    f.write(content)
