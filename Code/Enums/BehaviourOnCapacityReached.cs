namespace NTC.Pool
{
    public enum BehaviourOnCapacityReached
    {
        ReturnNullableClone,
        Instantiate,
        InstantiateWithCallbacks,
        Recycle,
        ThrowException
    }
}