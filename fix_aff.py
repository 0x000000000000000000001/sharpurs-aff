import sys
content = open('src/Effect/Aff.fs').read()

content = content.replace('member this.KillError = killError.Value.Value', 'member this.KillError = killError.Value')
content = content.replace('killError.Value.Value <- Some ex', 'killError.Value <- Some ex')

content = content.replace('let token = { Token = cts.Token; KillError = killError }', 'let token = { Token = cts.Token; KillError = ref None }')
content = content.replace('CancellationTokenSource.CreateLinkedTokenSource(ctx)', 'CancellationTokenSource.CreateLinkedTokenSource(ctx.Token)')
content = content.replace('cts.Cancel()', 'cts.Cancel()') # just checking

# Line 36: token for StartWithContinuations in _parAffAlt
content = content.replace('Async.StartWithContinuations(aff1 token, (fun r -> handleResult (Ok r)), (fun e -> handleResult (Error e)), (fun _ -> ()), token)', 'Async.StartWithContinuations(aff1 token, (fun r -> handleResult (Ok r)), (fun e -> handleResult (Error e)), (fun _ -> ()), cts.Token)')
content = content.replace('Async.StartWithContinuations(aff2 token, (fun r -> handleResult (Ok r)), (fun e -> handleResult (Error e)), (fun _ -> ()), token)', 'Async.StartWithContinuations(aff2 token, (fun r -> handleResult (Ok r)), (fun e -> handleResult (Error e)), (fun _ -> ()), cts.Token)')

# Line 36 etc: WaitAsync timeout cancellation token. Wait, what is on line 36?
open('src/Effect/Aff.fs', 'w').write(content)

content_test = open('test/Test/Main.purs').read()
open('test/Test/Main.purs', 'w').write(content_test)

