using UnityEngine;

namespace NTC.Pool
{
    internal static class Constants
    {
        internal const BehaviourOnCapacityReached DefaultBehaviourOnCapacityReached = BehaviourOnCapacityReached.InstantiateWithCallbacks;
        internal const DespawnType DefaultDespawnType = DespawnType.DeactivateAndHide;
        internal const CallbacksType DefaultCallbacksType = CallbacksType.Interfaces;
        internal const ReactionOnRepeatedDelayedDespawn DefaultDelayedDespawnHandleType = ReactionOnRepeatedDelayedDespawn.ResetDelay;
        internal const NightPoolMode DefaultNightPoolMode = NightPoolMode.Performance;
        internal const string HelpUrl = "https://github.com/MeeXaSiK/NightPool";
        internal const string NightPoolComponentPath = Author + "/Night Pool/";
        internal const string PoolsPresetComponentPath = NightPoolComponentPath + "Pools Preset";
        internal const string OnSpawnMessageName = "OnSpawn";
        internal const string OnDespawnMessageName = "OnDespawn";
        internal const int DefaultPoolsMapCapacity = 64;
        internal const int DefaultPersistentPoolsCapacity = 8;
        internal const int DefaultClonesCapacity = 128;
        internal const int DefaultDespawnRequestsCapacity = 32;
        internal const int DefaultPoolableInterfacesCapacity = 16;
        internal const int DefaultPoolCapacity = 32;
        internal const int DefaultPoolablesListCapacity = 32;
        internal const bool DefaultSendWarningsStatus = true;
        internal const bool DefaultPoolPersistenceStatus = false;
        internal const int NightPoolExecutionOrder = -10999;
        internal const int NewPoolPreloadSize = 0;
        private const string Author = "Night Train Code";

        internal static readonly Vector3 Vector3One = Vector3.one;
        internal static readonly Vector3 DefaultPosition = Vector3.zero;
        internal static readonly Quaternion DefaultRotation = Quaternion.identity;

        internal static class Tooltips
        {
            internal const string CallbacksType = "Whether to make callbacks when clones spawn and despawn? \n \n" +
                                              "None - disables callbacks; \n \n" +
                                              "Interfaces - finds the ISpawnable, IDespawnable or IPoolable interfaces using GetComponents and calls them; \n \n" +
                                              "Interfaces In Children - finds the ISpawnable, IDespawnable or IPoolable interfaces using GetComponentsInChildren and calls them; \n \n" +
                                              "Send Message - sends OnSpawn and OnDespawn messages by GameObject.SendMessage; \n \n" +
                                              "Broadcast Message - broadcasts OnSpawn and OnDespawn messages by GameObject.BroadcastMessage.";

            internal const string DespawnType = "How clones must be despawned? \n \n" +
                                                "Only Deactivate - deactivates clones and put them back in a pool; \n \n" +
                                                "Deactivate And Set Null Parent - does the same as the first one, but also changes the parent to null; \n \n" +
                                                "Deactivate And Hide - does the same as the first one, but also changes the parent to a pool.";

            internal const string OverflowBehaviour = "What to do when pool overflows? \n \n" +
                                             "Return Nullable Clone - returns nullable clone; \n \n" +
                                             "Instantiate - instantiates clone which will not be cached in a pool. Such clones ignore all callbacks; \n \n" +
                                             "Instantiate With Callbacks - instantiates clone which will not be cached in a pool; \n \n" +
                                             "Recycle - new clones force older ones to despawn; \n \n" +
                                             "Throw Exception - throws an exception.";
            
            internal const string Capacity = "Default capacity for runtime created pools.";
            internal const string Persistent = "Should pools created at runtime be persistent?";
            internal const string Warnings = "Should pools created at runtime find issues and log warnings by default?";
            internal const string DelayedDespawnReaction = "What will be done if you try to destroy the same clone multiple times with a delay? \n \n" +
                                                         "Ignore - ignores repeated delayed despawn of the clone; \n \n" +
                                                         "Reset Delay - resets the time of delayed despawn of the clone; \n \n" +
                                                         "Reset Delay If New Time Is Less - resets the time of delayed despawn of the clone if new time is less; \n \n" +
                                                         "Reset Delay If New Time Is Greater - resets the time of delayed despawn of the clone if new time is greater; \n \n" +
                                                         "Throw Exception - throws an exception.";

            internal const string NightPoolMode = "Performance - faster than Safety, but all pools must have lossy scale equlas Vector3.one; \n \n" +
                                                  "Safety - less performant, but allows pools to be set to any scale.";
            internal const string DespawnPersistentClonesOnDestroy = "Should persistent clones be despawned on destroy?";
            internal const string CheckClonesForNull = "Should clones be checked for null on spawn?";
            internal const string CheckForPrefab = "Should a pool prefab be checked during setup to see if it is really prefab?";
            internal const string ClearEventsOnDestroy = "Should NightPool static events be cleared on destroy?";
            internal const string GlobalUpdateType = "UpdateType of this component. Handles delayed despawns.";
            internal const string GlobalPreloadType = "Preload type of pools in a PoolsPreset below.";
            internal const string PoolsToPreload = "Pools to preload.";
        }
    }
}