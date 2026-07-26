using System;

namespace Effect.Aff;

public static class FFI {
    public static object _Pure(object arg1) => throw new NotImplementedException("Not implemented: _pure");
    public static object _ThrowError(object arg1) => throw new NotImplementedException("Not implemented: _throwError");
    public static object _CatchError(object arg1, object arg2) => throw new NotImplementedException("Not implemented: _catchError");
    public static object _Fork(object arg1, object arg2) => throw new NotImplementedException("Not implemented: _fork");
    public static object _Map(object arg1, object arg2) => throw new NotImplementedException("Not implemented: _map");
    public static object _Bind(object arg1, object arg2) => throw new NotImplementedException("Not implemented: _bind");
    public static object _Delay(object arg1, object arg2) => throw new NotImplementedException("Not implemented: _delay");
    public static object _LiftEffect(object arg1) => throw new NotImplementedException("Not implemented: _liftEffect");
    public static object _ParAffMap(object arg1, object arg2) => throw new NotImplementedException("Not implemented: _parAffMap");
    public static object _ParAffApply(object arg1, object arg2) => throw new NotImplementedException("Not implemented: _parAffApply");
    public static object _ParAffAlt(object arg1, object arg2) => throw new NotImplementedException("Not implemented: _parAffAlt");
    public static object _MakeFiber(object arg1, object arg2) => throw new NotImplementedException("Not implemented: _makeFiber");
    public static object _MakeSupervisedFiber(object arg1, object arg2) => throw new NotImplementedException("Not implemented: _makeSupervisedFiber");
    public static object _KillAll(object arg1, object arg2, object arg3) => throw new NotImplementedException("Not implemented: _killAll");
    public static object _Sequential(object arg1) => throw new NotImplementedException("Not implemented: _sequential");
    public static object GeneralBracket(object arg1, object arg2, object arg3) => throw new NotImplementedException("Not implemented: generalBracket");
    public static object MakeAff(object arg1) => throw new NotImplementedException("Not implemented: makeAff");
}
