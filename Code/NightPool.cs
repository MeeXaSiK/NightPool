using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NTC.Pool
{
    public static class NightPool
    {
        internal static readonly Dictionary<GameObject, Poolable> ClonesMap = 
            new Dictionary<GameObject, Poolable>(Constants.DefaultClonesCapacity);
        
        internal static readonly NightPoolList<DespawnRequest> DespawnRequests = 
            new NightPoolList<DespawnRequest>(Constants.DefaultDespawnRequestsCapacity);

        internal static NightPoolMode s_nightPoolMode = Constants.DefaultNightPoolMode;
        internal static bool s_hasTheNightPoolInitialized = false;
        internal static bool s_isApplicationQuitting = false;
        internal static bool s_despawnPersistentClonesOnDestroy = true;
        internal static bool s_checkClonesForNull = true;
        internal static bool s_checkForPrefab = true;
        internal static NightPoolGlobal s_instance;

        private static readonly Dictionary<GameObject, NightGameObjectPool> AllPoolsMap = 
            new Dictionary<GameObject, NightGameObjectPool>(Constants.DefaultPoolsMapCapacity);
        
        private static readonly Dictionary<GameObject, NightGameObjectPool> PersistentPoolsMap = 
            new Dictionary<GameObject, NightGameObjectPool>(Constants.DefaultPersistentPoolsCapacity);

        private static readonly List<ISpawnable> SpawnableItemComponents = 
            new List<ISpawnable>(Constants.DefaultPoolableInterfacesCapacity);
        
        private static readonly List<IDespawnable> DespawnableItemComponents = 
            new List<IDespawnable>(Constants.DefaultPoolableInterfacesCapacity);

        private static readonly object SecurityLock = new object();

        private static BehaviourOnCapacityReached BehaviourOnCapacityReached => s_hasTheNightPoolInitialized 
            ? s_instance._behaviourOnCapacityReached 
            : Constants.DefaultBehaviourOnCapacityReached;
        
        private static DespawnType DespawnType => s_hasTheNightPoolInitialized
            ? s_instance._despawnType
            : Constants.DefaultDespawnType;
        
        private static CallbacksType CallbacksType => s_hasTheNightPoolInitialized 
            ? s_instance._callbacksType 
            : Constants.DefaultCallbacksType;

        private static ReactionOnRepeatedDelayedDespawn ReactionOnRepeatedDelayedDespawn => s_hasTheNightPoolInitialized
            ? s_instance._reactionOnRepeatedDelayedDespawn
            : Constants.DefaultDelayedDespawnHandleType;
        
        private static int Capacity => s_hasTheNightPoolInitialized 
            ? s_instance._capacity 
            : Constants.DefaultPoolCapacity;
        
        private static bool Persistent => s_hasTheNightPoolInitialized 
            ? s_instance._dontDestroyOnLoad 
            : Constants.DefaultPoolPersistenceStatus;
        
        private static bool Warnings => s_hasTheNightPoolInitialized 
            ? s_instance._sendWarnings 
            : Constants.DefaultSendWarningsStatus;
        
        /// <summary>
        /// The actions will be performed on a game object created in any pool.
        /// </summary>
        public static readonly NightPoolEvent<GameObject> GameObjectInstantiated = new NightPoolEvent<GameObject>();
        
        /// <summary>
        /// Installs a pools by PoolPreset.
        /// </summary>
        public static void InstallPools(PoolsPreset poolsPreset)
        {
#if DEBUG
            if (poolsPreset == null)
                throw new ArgumentNullException(nameof(poolsPreset));
#endif
            int count = poolsPreset.Presets.Count;
            
            for (int i = 0; i < count; i++)
            {
                PoolPreset preset = poolsPreset.Presets[i];

                if (preset.Enabled == false)
                    continue;
                
                GameObject prefab = preset.Prefab;
#if DEBUG
                if (prefab == null)
                {
                    Debug.LogError($"The {nameof(PoolsPreset)} '{poolsPreset}' has one or more null prefabs!",
                        poolsPreset);
                    
                    continue;
                }
#endif
                int preloadSize = Mathf.Clamp(preset.PreloadSize, 0, preset.Capacity);

                if (TryGetPoolByPrefab(prefab, out NightGameObjectPool pool) == false)
                {
                    pool = CreateNewGameObjectPool(prefab);
                    
                    SetupNewPool(
                        pool, 
                        prefab, 
                        preset.BehaviourOnCapacityReached, 
                        preset.DespawnType,
                        preset.CallbacksType, 
                        preset.Capacity, 
                        preloadSize, 
                        preset.Persistent, 
                        preset.Warnings);
                }
                else
                {
                    if (preset.Persistent && pool.HasRegisteredAsPersistent)
                    {
                        continue;
                    }
#if DEBUG
                    Debug.LogError($"The pool '{pool}' you are trying to install by {nameof(PoolsPreset)} " +
                                   $"'{poolsPreset}' already exists!", pool);
#endif
                }
            }
        }

        /// <summary>
        /// Spawns a GameObject.
        /// </summary>
        /// <param name="prefab">GameObject prefab to spawn.</param>
        /// <returns>Spawned GameObject.</returns>
        public static GameObject Spawn(GameObject prefab)
        {
            Transform prefabTransform = prefab.transform;
            
            return DefaultSpawn(
                prefab, prefabTransform.localPosition, prefabTransform.localRotation, null, false, out _);
        }

        /// <summary>
        /// Spawns a GameObject.
        /// </summary>
        /// <param name="prefab">GameObject prefab to spawn.</param>
        /// <param name="position">Spawned GameObject position.</param>
        /// <param name="rotation">Spawned GameObject rotation.</param>
        /// <returns>Spawned GameObject.</returns>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return DefaultSpawn(prefab, position, rotation, null, false, out _);
        }
        
        /// <summary>
        /// Spawns a GameObject.
        /// </summary>
        /// <param name="prefab">GameObject prefab to spawn.</param>
        /// <param name="position">Spawned GameObject position.</param>
        /// <param name="rotation">Spawned GameObject rotation.</param>
        /// <param name="parent">The parent of the spawned GameObject.</param>
        /// <returns>Spawned GameObject.</returns>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (parent != null)
            {
                position = parent.InverseTransformPoint(position);
                rotation = Quaternion.Inverse(parent.rotation) * rotation;
            }
            
            return DefaultSpawn(prefab, position, rotation, parent, false, out _);
        }

        /// <summary>
        /// Spawns a GameObject.
        /// </summary>
        /// <param name="prefab">GameObject prefab to spawn.</param>
        /// <param name="parent">The parent of the spawned GameObject.</param>
        /// <param name="worldPositionStays">World position stays.</param>
        /// <returns>Spawned GameObject.</returns>
        public static GameObject Spawn(GameObject prefab, Transform parent, bool worldPositionStays = false)
        {
            GetPositionAndRotationByParent(prefab, parent, out Vector3 position, out Quaternion rotation);

            return DefaultSpawn(prefab, position, rotation, parent, worldPositionStays, out _);
        }
        
        /// <summary>
        /// Spawns a GameObject as T component.
        /// </summary>
        /// <param name="prefab">Component prefab to spawn.</param>
        /// <typeparam name="T">Component.</typeparam>
        /// <returns>Spawned GameObject as T component.</returns>
        public static T Spawn<T>(T prefab) where T : Component
        {
            Transform prefabTransform = prefab.transform;
            
            GameObject spawnedGameObject = DefaultSpawn(prefab.gameObject, prefabTransform.localPosition,
                prefabTransform.localRotation, null, false, out bool haveToGetComponent);
            
            return haveToGetComponent 
                ? spawnedGameObject.GetComponent<T>() 
                : null;
        }

        /// <summary>
        /// Spawns a GameObject as T component.
        /// </summary>
        /// <param name="prefab">Component prefab to spawn.</param>
        /// <param name="position">Spawned GameObject position.</param>
        /// <param name="rotation">Spawned GameObject rotation.</param>
        /// <typeparam name="T">Component type.</typeparam>
        /// <returns>Spawned GameObject as T component.</returns>
        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) 
            where T : Component
        {
            GameObject spawnedGameObject = DefaultSpawn(prefab.gameObject, position, rotation, null, false, 
                out bool haveToGetComponent);

            return haveToGetComponent 
                ? spawnedGameObject.GetComponent<T>() 
                : null;
        }
        
        /// <summary>
        /// Spawns a GameObject as T component.
        /// </summary>
        /// <param name="prefab">Component prefab to spawn.</param>
        /// <param name="parent">The parent of the spawned GameObject.</param>
        /// <param name="position">Spawned GameObject position.</param>
        /// <param name="rotation">Spawned GameObject rotation.</param>
        /// <typeparam name="T">Component type.</typeparam>
        /// <returns>Spawned GameObject as T component.</returns>
        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) 
            where T : Component
        {
            if (parent != null)
            {
                position = parent.InverseTransformPoint(position);
                rotation = Quaternion.Inverse(parent.rotation) * rotation;
            }
            
            GameObject spawnedGameObject = DefaultSpawn(prefab.gameObject, position, rotation, parent, false, 
                out bool haveToGetComponent);

            return haveToGetComponent 
                ? spawnedGameObject.GetComponent<T>() 
                : null;
        }
        
        /// <summary>
        /// Spawns a GameObject as T component.
        /// </summary>
        /// <param name="prefab">Component prefab to spawn.</param>
        /// <param name="parent">The parent of the spawned GameObject.</param>
        /// <param name="worldPositionStays">World position stays.</param>
        /// <typeparam name="T">Component type.</typeparam>
        /// <returns>Spawned GameObject as T component.</returns>
        public static T Spawn<T>(T prefab, Transform parent, bool worldPositionStays = false) where T : Component
        {
            GameObject prefabGameObject = prefab.gameObject;
            
            GetPositionAndRotationByParent(prefabGameObject, parent, out Vector3 position, out Quaternion rotation);
            
            GameObject spawnedGameObject = DefaultSpawn(prefabGameObject, position, rotation, parent, 
                worldPositionStays, out bool haveToGetComponent);

            return haveToGetComponent 
                ? spawnedGameObject.GetComponent<T>() 
                : null;
        }

        /// <summary>
        /// Despawns the clone.
        /// </summary>
        /// <param name="clone">Clone to despawn.</param>
        /// <param name="delay">Despawn delay.</param>
        public static void Despawn(Component clone, float delay = 0f)
        {
            DefaultDespawn(clone.gameObject, delay);
        }
        
        /// <summary>
        /// Despawns the clone.
        /// </summary>
        /// <param name="clone">Clone to despawn.</param>
        /// <param name="delay">Despawn delay.</param>
        public static void Despawn(GameObject clone, float delay = 0f)
        {
            DefaultDespawn(clone, delay);
        }

        /// <summary>
        /// Performs an action for each pool.
        /// </summary>
        /// <param name="action">Action to perform.</param>
        /// <exception cref="ArgumentNullException">Throws if action is null.</exception>
        public static void ForEachPool(Action<NightGameObjectPool> action)
        {
#if DEBUG
            if (action == null)
                throw new ArgumentNullException(nameof(action));
#endif
            foreach (NightGameObjectPool pool in AllPoolsMap.Values)
            {
                action.Invoke(pool);
            }
        }

        /// <summary>
        /// Performs an action for each clone.
        /// </summary>
        /// <param name="action">Action to perform.</param>
        /// <exception cref="ArgumentNullException">Throws if action is null.</exception>
        public static void ForEachClone(Action<GameObject> action)
        {
#if DEBUG
            if (action == null)
                throw new ArgumentNullException(nameof(action));
#endif
            foreach (Poolable poolable in ClonesMap.Values)
            {
                action.Invoke(poolable._gameObject);
            }
        }
        
        /// <summary>
        /// Tries to get pool by spawned gameObject.
        /// </summary>
        /// <param name="clone">Component which spawned via NightPool</param>
        /// <param name="pool">Found pool.</param>
        /// <returns>Returns true if pool found, otherwise false.</returns>
        public static bool TryGetPoolByClone(Component clone, out NightGameObjectPool pool)
        {
            return TryGetPoolByClone(clone.gameObject, out pool);
        }
        
        /// <summary>
        /// Tries to get pool by spawned gameObject.
        /// </summary>
        /// <param name="clone">GameObject which spawned via NightPool</param>
        /// <param name="pool">Found pool.</param>
        /// <returns>Returns true if pool found, otherwise false.</returns>
        public static bool TryGetPoolByClone(GameObject clone, out NightGameObjectPool pool)
        {
            if (ClonesMap.TryGetValue(clone, out Poolable poolable) && poolable._isSetup)
            {
                pool = poolable._pool;
                return true;
            }
            
            pool = null;
            return false;
        }
        
        /// <summary>
        /// Tries to get pool by gameObject prefab.
        /// </summary>
        /// <param name="prefab">Component prefab.</param>
        /// <param name="pool">Found pool.</param>
        /// <returns>Returns true if pool found, otherwise false.</returns>
        public static bool TryGetPoolByPrefab(Component prefab, out NightGameObjectPool pool)
        {
            return TryGetPoolByPrefab(prefab.gameObject, out pool);
        }

        /// <summary>
        /// Tries to get pool by gameObject prefab.
        /// </summary>
        /// <param name="prefab">GameObject prefab.</param>
        /// <param name="pool">Found pool.</param>
        /// <returns>Returns true if pool found, otherwise false.</returns>
        public static bool TryGetPoolByPrefab(GameObject prefab, out NightGameObjectPool pool)
        {
            return AllPoolsMap.TryGetValue(prefab, out pool);
        }

        /// <summary>
        /// Returns the pool by clone.
        /// </summary>
        /// <param name="clone">Component which spawned via NightPool</param>
        /// <returns>Found pool.</returns>
        public static NightGameObjectPool GetPoolByClone(Component clone)
        {
            return GetPoolByClone(clone.gameObject);
        }
        
        /// <summary>
        /// Returns the pool by clone.
        /// </summary>
        /// <param name="clone">GameObject which spawned via NightPool</param>
        /// <returns>Found pool.</returns>
        public static NightGameObjectPool GetPoolByClone(GameObject clone)
        {
            var hasPool = TryGetPoolByClone(clone, out NightGameObjectPool pool);
#if DEBUG
            if (hasPool == false)
                Debug.LogError($"The pool was not found by the clone '{clone}'!", clone);
#endif
            return pool;
        }
        
        /// <summary>
        /// Returns the pool by prefab.
        /// </summary>
        /// <param name="prefab">Component's prefab.</param>
        /// <returns>Found pool.</returns>
        public static NightGameObjectPool GetPoolByPrefab(Component prefab)
        {
            return GetPoolByPrefab(prefab.gameObject);
        }
        
        /// <summary>
        /// Returns the pool by prefab.
        /// </summary>
        /// <param name="prefab">GameObject's prefab.</param>
        /// <returns>Found pool.</returns>
        public static NightGameObjectPool GetPoolByPrefab(GameObject prefab)
        {
            var hasPool = TryGetPoolByPrefab(prefab, out NightGameObjectPool pool);
#if DEBUG
            if (hasPool == false)
                Debug.LogError($"The pool was not found by the prefab '{prefab}'!", prefab);
#endif
            return pool;
        }

        /// <summary>
        /// Is the component a clone (spawned using NightPool)?
        /// </summary>
        /// <param name="clone">Component to check.</param>
        /// <returns>True if component is a clone of the prefab, otherwise false.</returns>
        public static bool IsClone(Component clone)
        {
            return IsClone(clone.gameObject);
        }
        
        /// <summary>
        /// Is the game object a clone (spawned using NightPool)?
        /// </summary>
        /// <param name="clone">GameObject to check.</param>
        /// <returns>True if game object is a clone of the prefab, otherwise false.</returns>
        public static bool IsClone(GameObject clone)
        {
            return ClonesMap.ContainsKey(clone);
        }

        /// <summary>
        /// Returns the status of the clone.
        /// </summary>
        /// <param name="clone">Component which spawned via NightPool</param>
        /// <returns>Status of the clone.</returns>
        public static PoolableStatus GetCloneStatus(Component clone)
        {
            return GetCloneStatus(clone.gameObject);
        }
        
        /// <summary>
        /// Returns the status of the clone.
        /// </summary>
        /// <param name="clone">GameObject which spawned via NightPool</param>
        /// <returns>Status of the clone.</returns>
        public static PoolableStatus GetCloneStatus(GameObject clone)
        {
            if (ClonesMap.TryGetValue(clone.gameObject, out Poolable poolable))
            {
                return poolable._status;
            }
#if DEBUG
            Debug.LogError($"The clone '{clone}' is not a poolable!", clone);
#endif
            return default;
        }

        /// <summary>
        /// Destroys a clone.
        /// </summary>
        /// <param name="clone">Component which spawned via NightPool</param>
        public static void DestroyClone(Component clone)
        {
            DestroyPoolableWithGameObject(clone.gameObject, false);
        }

        /// <summary>
        /// Destroys a clone.
        /// </summary>
        /// <param name="clone">GameObject which spawned via NightPool</param>
        public static void DestroyClone(GameObject clone)
        {
            DestroyPoolableWithGameObject(clone, false);
        }

        /// <summary>
        /// Destroys a clone immediately.
        /// </summary>
        /// <param name="clone">GameObject which spawned via NightPool</param>
        public static void DestroyCloneImmediate(Component clone)
        {
            DestroyPoolableWithGameObject(clone.gameObject, true);
        }
        
        /// <summary>
        /// Destroys a clone immediately.
        /// </summary>
        /// <param name="clone">GameObject which spawned via NightPool</param>
        public static void DestroyCloneImmediate(GameObject clone)
        {
            DestroyPoolableWithGameObject(clone, true);
        }

        /// <summary>
        /// Destroys all pools.
        /// </summary>
        /// <param name="immediately">Should all pools be destroyed immediately?</param>
        public static void DestroyAllPools(bool immediately = false)
        {
            if (CanPerformPoolAction() == false)
            {
#if DEBUG
                Debug.LogError("You are trying to destroy all pools when application is quitting!");
#endif
                return;
            }

            if (immediately)
                ForEachPool(pool => pool.DestroyPoolImmediate());
            else
                ForEachPool(pool => pool.DestroyPool());
        }
        
        internal static void RegisterPool(NightGameObjectPool pool)
        {
            if (AllPoolsMap.ContainsKey(pool._prefab) == false)
            {
                AllPoolsMap.Add(pool._prefab, pool);
            }
#if DEBUG
            else
            {
                Debug.LogError($"You are trying to register another pool '{pool.name}' " +
                               $"with the same prefab '{pool._prefab}'!", pool);
            }
#endif
        }
        
        internal static void UnregisterPool(NightGameObjectPool pool)
        {
            if (pool._isSetup == false)
                return;

            if (pool._dontDestroyOnLoad)
                PersistentPoolsMap.Remove(pool._prefab);
            
            AllPoolsMap.Remove(pool._prefab);
        }

        internal static void RegisterPersistentPool(NightGameObjectPool pool)
        {
            if (pool._dontDestroyOnLoad)
            {
                if (PersistentPoolsMap.ContainsKey(pool._prefab) == false)
                {
                    PersistentPoolsMap.Add(pool._prefab, pool);
                }
#if DEBUG
                else
                {
                    if (pool._sendWarnings)
                    {
                        Debug.LogWarning($"You are trying to register the persistent pool '{pool.name}' twice!", pool);
                    }
                }
#endif
            }
        }

        internal static bool HasPoolRegisteredAsPersistent(NightGameObjectPool pool)
        {
            return PersistentPoolsMap.ContainsKey(pool._prefab);
        }

        internal static void DespawnImmediate(Poolable poolable)
        {
            if (poolable._isSetup)
            {
                if (poolable._status == PoolableStatus.SpawnedOverCapacity)
                {
                    if (poolable._pool._behaviourOnCapacityReached == BehaviourOnCapacityReached.InstantiateWithCallbacks)
                    {
                        RaiseCallbacksOnDespawn(poolable);
                    }
                    
                    poolable.Dispose(true);
                    return;
                }
                
                RaiseCallbacksOnDespawn(poolable);
                
                poolable._pool.Release(poolable);
                poolable._pool.RaiseGameObjectDespawnedCallback(poolable._gameObject);
                poolable._status = PoolableStatus.Despawned;
            }
            else
            {
#if DEBUG
                if (Warnings)
                {
                    Debug.LogWarning($"The poolable '{poolable._gameObject}' was not setup and will be destroyed!", 
                        poolable._gameObject);
                }
#endif
                poolable.Dispose(true);
            }
        }
        
        internal static void ResetPool()
        {
            ResetLists();
            ResetClonesDictionary();
            HandlePersistentPoolsOnDestroy();
            s_hasTheNightPoolInitialized = false;
        }

        private static void RaiseCallbacksOnSpawn(Poolable poolable)
        {
            if (poolable._pool._callbacksType == CallbacksType.None)
                return;
            
            InvokeCallbacks(
                poolable._gameObject, 
                poolable._pool._callbacksType, 
                spawnable => spawnable.OnSpawn(), 
                SpawnableItemComponents, 
                Constants.OnSpawnMessageName);
        }
        
        private static void RaiseCallbacksOnDespawn(Poolable poolable)
        {
            if (poolable._pool._callbacksType == CallbacksType.None)
                return;
            
            InvokeCallbacks(
                poolable._gameObject, 
                poolable._pool._callbacksType, 
                despawnable => despawnable.OnDespawn(), 
                DespawnableItemComponents, 
                Constants.OnDespawnMessageName);
        }

        private static void InitializeTheNightPool()
        {
            lock (SecurityLock)
            {
                if (s_instance == null)
                {
                    if (TryFindNightPoolInstanceAsSingle(out s_instance) == false)
                    {
                        CreateNightPoolInstance();
#if DEBUG
                        Debug.LogWarning($"The <{nameof(NightPoolGlobal)}> instance was created automatically. " +
                                         $"Add this component on any {nameof(GameObject)} in the scene manually " +
                                         "to avoid some problems.");
#endif
                    }
                }

                s_hasTheNightPoolInitialized = true;
            }
        }

        private static bool TryFindNightPoolInstanceAsSingle(out NightPoolGlobal nightPool)
        {
            var instances = Object.FindObjectsOfType<NightPoolGlobal>();
            var length = instances.Length;

            if (length > 0)
            {
#if DEBUG
                if (length > 1)
                {
                    for (var i = 1; i < length; i++)
                    {
                        Object.Destroy(instances[i]);
                    }
                    
                    Debug.LogError(
                        $"The number of the {nameof(NightPoolGlobal)} instances in the scene is greater than one!");
                }
#endif
                nightPool = instances[0];
                return true;
            }

            nightPool = null;
            return false;
        }
        
        private static NightGameObjectPool GetPoolByPrefabOrCreate(GameObject prefab)
        {
            if (TryGetPoolByPrefab(prefab, out NightGameObjectPool pool) == false)
            {
                pool = CreateNewGameObjectPool(prefab);

                SetupNewPool(
                    pool, 
                    prefab, 
                    BehaviourOnCapacityReached, 
                    DespawnType,
                    CallbacksType, 
                    Capacity, 
                    Constants.NewPoolPreloadSize, 
                    Persistent, 
                    Warnings);
            }

            return pool;
        }
        
        private static void CreateNightPoolInstance()
        {
            s_instance = new GameObject("Night Pool Global").AddComponent<NightPoolGlobal>();
        }

        private static NightGameObjectPool CreateNewGameObjectPool(GameObject prefab)
        {
            return new GameObject($"[{nameof(NightPool)}] {prefab.name}").AddComponent<NightGameObjectPool>();
        }

        private static void SetupNewPool(
            NightGameObjectPool pool, 
            GameObject prefab, 
            BehaviourOnCapacityReached behaviourOnCapacityReached, 
            DespawnType despawnType,
            CallbacksType callbacksType, 
            int capacity, 
            int preloadSize, 
            bool persistent, 
            bool warnings)
        {
            pool._dontDestroyOnLoad = persistent;
            pool.SetWarningsActive(warnings);
            pool.SetCapacity(capacity);
            pool.SetCallbacksType(callbacksType);
            pool.SetDespawnType(despawnType);
            pool.SetBehaviourOnCapacityReached(behaviourOnCapacityReached);
            pool.TrySetup(prefab);
            pool.PopulatePool(preloadSize);
        }

        private static GameObject DefaultSpawn(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent, bool worldPositionStays, out bool haveToGetComponent)
        {
            if (CanPerformPoolAction() == false)
            {
#if DEBUG
                Debug.LogError(
                    $"You are trying to spawn a prefab '{prefab}' when the application is quitting!", prefab);
#endif
                haveToGetComponent = false;
                return null;
            }

            NightGameObjectPool pool = GetPoolByPrefabOrCreate(prefab);
            pool.Get(out GettingPoolableArguments arguments);

            if (arguments.IsResultNullable)
            {
                haveToGetComponent = false;
                return null;
            }
#if DEBUG
            if (s_checkClonesForNull)
            {
                if (arguments.Poolable._gameObject == null)
                {
                    Debug.LogError("You are trying to spawn a clone that has been destroyed " +
                                   $"without the {nameof(NightPool)}! Prefab: '{prefab}'", pool);
                }
            }
#endif
            if (arguments.Poolable._status == PoolableStatus.Despawned)
            {
                arguments.Poolable._gameObject.SetActive(true);
            }

            SetupTransform(arguments.Poolable, pool, position, rotation, parent, worldPositionStays);
            pool.RaiseGameObjectSpawnedCallback(arguments.Poolable._gameObject);
            
            if (arguments.Poolable._status == PoolableStatus.SpawnedOverCapacity)
            {
                if (pool._behaviourOnCapacityReached == BehaviourOnCapacityReached.InstantiateWithCallbacks)
                {
                    RaiseCallbacksOnSpawn(arguments.Poolable);
                }
            }
            else
            {
                arguments.Poolable._status = PoolableStatus.Spawned;
                RaiseCallbacksOnSpawn(arguments.Poolable);
            }

            haveToGetComponent = true;
            return arguments.Poolable._gameObject;
        }

        private static void DefaultDespawn(GameObject gameObject, float delay = 0f)
        {
            if (CanPerformPoolAction() == false)
            {
#if DEBUG
                Debug.LogError(
                    $"You are trying to despawn the '{gameObject}' when the application is quitting!", gameObject);
#endif
                return;
            }

            if (ClonesMap.TryGetValue(gameObject, out Poolable poolable))
            {
                if (poolable._status == PoolableStatus.Despawned)
                {
#if DEBUG
                    if (poolable._pool._sendWarnings)
                    {
                        Debug.LogWarning("The game object you want to despawn has been already despawned!", gameObject);
                    }
#endif
                    return;
                }
                
                if (delay > 0f)
                {
                    DespawnWithDelay(poolable, delay);
                }
                else
                {
                    DespawnImmediate(poolable);
                }
            }
            else
            {
#if DEBUG
                if (Warnings)
                {
                    Debug.LogWarning($"The '{gameObject}' was not spawned with the {nameof(NightPool)} " +
                                     "(or the pool was destroyed) and will be destroyed!", gameObject);
                }
#endif
                Object.Destroy(gameObject, delay);
            }
        }

        private static void DespawnWithDelay(Poolable poolable, float delay)
        {
            ReactionOnRepeatedDelayedDespawn reaction = ReactionOnRepeatedDelayedDespawn;
                    
            if (reaction == ReactionOnRepeatedDelayedDespawn.Ignore)
            {
                CreateDespawnRequest(poolable, delay);
            }
            else
            {
                if (HasDespawnRequest(poolable, out int index))
                {
                    ref DespawnRequest request = ref DespawnRequests._components[index];
                            
                    switch (reaction)
                    {
                        case ReactionOnRepeatedDelayedDespawn.ResetDelay:
                            ResetDespawnDelay(ref request, delay);
                            break;
                        case ReactionOnRepeatedDelayedDespawn.ResetDelayIfNewTimeIsLess:
                            ResetDespawnDelayIfNewTimeIsLess(ref request, delay);
                            break;
                        case ReactionOnRepeatedDelayedDespawn.ResetDelayIfNewTimeIsGreater:
                            ResetDespawnDelayIfNewTimeIsGreater(ref request, delay);
                            break;
                        case ReactionOnRepeatedDelayedDespawn.ThrowException:
#if DEBUG
                            if (HasDespawnRequest(poolable, out _))
                            {
                                Debug.LogException(new Exception(
                                    "Delayed despawn request already exists for this clone!"), poolable._gameObject);
                            }       
#endif
                            break;
                    }
                }
                else
                {
                    CreateDespawnRequest(poolable, delay);
                }
            }
        }
        
        private static bool HasDespawnRequest(Poolable poolable, out int id)
        {
            for (int i = 0; i < DespawnRequests._count; i++)
            {
                if (DespawnRequests._components[i].Poolable == poolable)
                {
                    id = i;
                    return true;
                }
            }

            id = default;
            return false;
        }

        private static void CreateDespawnRequest(Poolable poolable, float delay)
        {
            DespawnRequests.Add(new DespawnRequest
            {
                Poolable = poolable,
                TimeToDespawn = delay
            });
        }

        private static void ResetDespawnDelay(ref DespawnRequest request, float delay)
        {
            request.TimeToDespawn = delay;
        }
        
        private static void ResetDespawnDelayIfNewTimeIsLess(ref DespawnRequest request, float delay)
        {
            if (delay < request.TimeToDespawn)
            {
                request.TimeToDespawn = delay;
            }
        }
        
        private static void ResetDespawnDelayIfNewTimeIsGreater(ref DespawnRequest request, float delay)
        {
            if (delay > request.TimeToDespawn)
            {
                request.TimeToDespawn = delay;
            }
        }
        
        private static bool CanPerformPoolAction()
        {
            if (s_isApplicationQuitting)
            {
#if UNITY_EDITOR
                if (EditorSettings.enterPlayModeOptionsEnabled && s_instance == null)
                {
                    throw new Exception($"The <{nameof(NightPoolGlobal)}> instance is null! " +
                                        "Enable 'Reload Domain' option in 'Enter Play Mode Options' or " +
                                        $"add this component on any {nameof(GameObject)} in the scene manually " +
                                        "to fix this problem.");
                }
#endif
                return false;
            }
            
            if (s_hasTheNightPoolInitialized == false)
            {
#if DEBUG
                if (Application.isPlaying == false)
                {
                    throw new Exception(
                        "You are trying to perform spawn or despawn when the application is not playing!");
                } 
#endif
                InitializeTheNightPool();
            }

            return true;
        }

        private static void GetPositionAndRotationByParent(GameObject prefab, Transform parent, 
            out Vector3 position, out Quaternion rotation)
        {
            if (parent != null)
            {
                Transform prefabTransform = prefab.transform;
                
                position = prefabTransform.position;
                rotation = prefabTransform.rotation;
            }
            else
            {
                position = Constants.DefaultPosition;
                rotation = Constants.DefaultRotation;
            }
        }
        
        private static void SetupTransform(Poolable poolable, NightGameObjectPool pool, Vector3 position, 
            Quaternion rotation, Transform parent = null, bool worldPositionStays = false)
        {
            if (s_nightPoolMode == NightPoolMode.Safety)
            {
                SetPoolableNullParent(poolable);
            }
            else
            {
                CheckPoolableForLightweightTransformSetup(pool, poolable);
            }
            
            poolable._transform.localScale = pool._regularPrefabScale;
            poolable._transform.SetPositionAndRotation(position, rotation);
            poolable._transform.SetParent(parent, worldPositionStays);
        }

        private static void CheckPoolableForLightweightTransformSetup(NightGameObjectPool pool, Poolable poolable)
        {
            if (pool._behaviourOnCapacityReached == BehaviourOnCapacityReached.Recycle)
            {
                SetPoolableNullParent(poolable);
                return;
            }
            
            if (pool._despawnType == DespawnType.OnlyDeactivate)
            {
                SetPoolableNullParent(poolable);
                return;
            }
#if DEBUG
            if (poolable._pool._cachedTransform.lossyScale != Constants.Vector3One)
            {
                Debug.LogError("The pool and its parents must have the same scale equal to 'Vector3.one' " +
                               $"in the Night Pool '{nameof(NightPoolMode.Performance)}' mode!", poolable._pool);
                
                SetPoolableNullParent(poolable);
            }
#endif
        }

        private static void SetPoolableNullParent(Poolable poolable)
        {
            poolable._transform.SetParent(null, false);
        }
        
        private static void InvokeCallbacks<T>(GameObject gameObject, CallbacksType callbacksType, 
            Action<T> poolableCallback, List<T> listForComponentsCaching, string messageKey)
        {
            switch (callbacksType)
            {
                case CallbacksType.Interfaces: 
                    InvokeGameObjectPoolEvents(gameObject, listForComponentsCaching, 
                        poolableCallback, inChildren: false);
                    break;
                case CallbacksType.InterfacesInChildren:
                    InvokeGameObjectPoolEvents(gameObject, listForComponentsCaching, 
                        poolableCallback, inChildren: true);
                    break;
                case CallbacksType.SendMessage:
                    gameObject.SendMessage(messageKey, SendMessageOptions.DontRequireReceiver);
                    break;
                case CallbacksType.BroadcastMessage:
                    gameObject.BroadcastMessage(messageKey, SendMessageOptions.DontRequireReceiver);
                    break;
                case CallbacksType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(callbacksType));
            }
        }

        private static void InvokeGameObjectPoolEvents<T>(GameObject gameObject, List<T> listForComponentCaching, 
            Action<T> callback, bool inChildren)
        {
            if (inChildren)
                gameObject.GetComponentsInChildren(listForComponentCaching);
            else
                gameObject.GetComponents(listForComponentCaching);

            int count = listForComponentCaching.Count;
            
            for (int i = 0; i < count; i++)
            {
                callback.Invoke(listForComponentCaching[i]);
            }
        }
        
        private static void DestroyPoolableWithGameObject(GameObject clone, bool immediately)
        {
            if (ClonesMap.TryGetValue(clone, out Poolable poolable))
            {
                if (poolable._isSetup)
                {
                    poolable._pool.UnregisterPoolable(poolable);
                    poolable.Dispose(immediately);
                }
#if DEBUG
                else
                {
                    Debug.LogError($"The clone '{clone}' is not setup!", clone);
                }
#endif
            }
            else
            {
#if DEBUG
                Debug.LogWarning($"The clone '{clone}' was not spawned by the {nameof(NightPool)}!", clone);
#endif
                Object.Destroy(clone);
            }
        }

        private static void ResetLists()
        {
            ClearListAndSetCapacity(SpawnableItemComponents, Constants.DefaultPoolableInterfacesCapacity);
            ClearListAndSetCapacity(DespawnableItemComponents, Constants.DefaultPoolableInterfacesCapacity);
            ClearListAndSetCapacity(DespawnRequests, Constants.DefaultDespawnRequestsCapacity);
        }

        private static void HandlePersistentPoolsOnDestroy()
        {
            if (s_isApplicationQuitting)
                return;
            
            if (s_despawnPersistentClonesOnDestroy == false)
                return;
            
            if (PersistentPoolsMap.Count == 0)
                return;
            
            foreach (NightGameObjectPool persistentPool in PersistentPoolsMap.Values)
            {
                persistentPool.DespawnAllClones();
            }
        }
        
        private static void ResetClonesDictionary()
        {
            if (s_isApplicationQuitting)
            {
                ClonesMap.Clear();
            }
        }

        private static void ClearListAndSetCapacity<T>(List<T> list, int capacity)
        {
            list.Clear();
            list.Capacity = capacity;
        }

        private static void ClearListAndSetCapacity(NightPoolList<DespawnRequest> list, int capacity)
        {
            list.Clear();
            list.SetCapacity(capacity);
        }
    }
}

#if ENABLE_IL2CPP
namespace Unity.IL2CPP.CompilerServices
{
    internal enum Option
    {
        NullChecks = 1,
        ArrayBoundsChecks = 2,
        DivideByZeroChecks = 3,
    }

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Delegate, Inherited = false, AllowMultiple = true)]
    internal class Il2CppSetOptionAttribute : Attribute
    {
        public Option Option { get; private set; }
        public object Value { get; private set; }

        public Il2CppSetOptionAttribute(Option option, object value)
        {
            Option = option;
            Value = value;
        }
    }
}
#endif