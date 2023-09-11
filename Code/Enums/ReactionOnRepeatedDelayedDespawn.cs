namespace NTC.Pool
{
    internal enum ReactionOnRepeatedDelayedDespawn
    {
        Ignore,
        ResetDelay,
        ResetDelayIfNewTimeIsLess,
        ResetDelayIfNewTimeIsGreater,
        ThrowException
    }
}