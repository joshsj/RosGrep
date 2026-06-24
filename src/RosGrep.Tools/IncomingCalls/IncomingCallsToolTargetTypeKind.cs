namespace RosGrep.Tools.IncomingCalls;

// todo should also support a property/method/field directly, not just complex types
public enum IncomingCallsToolTargetTypeKind
{
    Interface = 1,
    Class,
    Struct,
}