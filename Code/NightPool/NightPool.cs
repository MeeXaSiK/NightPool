// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021-2022 Night Train Code
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using static NTC.Global.System.Tasks.TaskSugar;
using Object = UnityEngine.Object;

namespace NTC.Global.Pool
{
    public static class NightPool
    {
        /// <summary>
        /// Event for each spawned GameObject
        /// </summary>
        public static event Action<GameObject> OnObjectSpawned;
        
        /// <summary>
        /// Event for each despawned GameObject
        /// </summary>
        public static event Action<GameObject> OnObjectDespawned;
        
        /// <summary>
        /// List of all pools
        /// </summary>
        private static readonly List<Pool> AllPools = new List<Pool>(64);
        
        /// <summary>
        /// List to search for all IPoolItem components on GameObject
        /// </summary>
        private static readonly List<IPoolItem> ItemEventComponents = new List<IPoolItem>(32);

        /// <summary>
        /// Allows you to determine whether the game is running or the Unity is open
        /// </summary>
        private static bool IsEditor => Application.isPlaying == false;

        /// <summary>
        /// Installs pool items by PoolPreset
        /// </summary>
        public static void InstallPoolItems(PoolPreset poolPreset)
        {
            if (poolPreset == null || IsEditor)
                return;
            
            foreach (var poolItem in poolPreset.PoolItems)
            {
                var prefab = poolItem.Prefab;
                var pool = GetPoolByPrefab(prefab);
                
                pool.PopulatePool(poolItem.Size);
            }
        }

        public static T Spawn<T>(T component, Vector3 position = default, Quaternion rotation = default) 
            where T : Component
        {
            return 
                DefaultSpawn(component.gameObject, position, rotation, null, false).
                GetComponent<T>();
        }
        
        public static T Spawn<T>(T component, Transform parent, Quaternion rotation = default, 
            bool worldStaysPosition = false) where T : Component
        {
            var position = parent != null 
                ? parent.position 
                : Vector3.zero;
            
            return 
                DefaultSpawn(component.gameObject, position, rotation, parent, worldStaysPosition).
                GetComponent<T>();
        }

        public static GameObject Spawn(GameObject toSpawn, Vector3 position = default, Quaternion rotation = default)
        {
            return DefaultSpawn(toSpawn, position, rotation, null, false);
        }
        
        public static GameObject Spawn(GameObject toSpawn, Transform parent, Quaternion rotation = default, 
            bool worldPositionStays = false)
        {
            var position = parent != null 
                ? parent.position 
                : Vector3.zero;
            
            return DefaultSpawn(toSpawn, position, rotation, parent, worldPositionStays);
        }

        public static void Despawn(Component toDespawn, float delay = 0f)
        {
            DefaultDespawn(toDespawn.gameObject, delay);
        }

        public static void Despawn(GameObject toDespawn, float delay = 0f)
        {
            DefaultDespawn(toDespawn, delay);
        }

        /// <summary>
        /// Destroys pool by GameObject
        /// </summary>
        public static void DestroyPool(GameObject gameObject)
        {
            if (gameObject == null)
            {
#if DEBUG
                Debug.LogWarning("GameObject is null!");
#endif
                return;
            }
            
            if (gameObject.TryGetComponent(out Poolable poolable))
            {
                DestroyPool(poolable.Pool);
            }
            else
            {
#if DEBUG
                Debug.LogError($"{gameObject.name} was not spawned by NightPool!");
#endif
            }
        }

        public static void DestroyPool(Pool pool)
        {
            if (pool == null)
            {
#if DEBUG
                Debug.LogWarning("Pool is null!");
#endif
                return;
            }
            
            foreach (var poolable in pool.Poolables)
            {
                Object.Destroy(poolable.gameObject);
            }

            Object.Destroy(pool.gameObject);
            
            AllPools.Remove(pool);
        }
        
        /// <summary>
        /// Destroys all pools
        /// </summary>
        public static void DestroyAllPools()
        {
            if (IsEditor) 
                return;

            var pools = AllPools.ToArray();
            
            foreach (var pool in pools)
            {
                DestroyPool(pool);
            }
            
            AllPools.Clear();
        }
        
        /// <summary>
        /// Resets the static components of NightPool
        /// </summary>
        public static void Reset()
        {
            ResetLists();
            
            ResetActions();
        }

        /// <summary>
        /// Allows to search for the requires pool by prefab of GameObject
        /// </summary>
        /// <returns> Pool by prefab if exists one. Otherwise, it will create a new </returns>
        public static Pool GetPoolByPrefab(GameObject prefab)
        {
            var count = AllPools.Count;
            
            for (var i = 0; i < count; i++)
            {
                if (AllPools[i].Prefab == prefab)
                {
                    return AllPools[i];
                }
            }

            return CreateNewPool(prefab);
        }

        /// <summary>
        /// Creates a new pool
        /// </summary>
        /// <returns> A new pool </returns>
        private static Pool CreateNewPool(GameObject prefab)
        {
            var poolParent = new GameObject($"[NightPool] {prefab.name}");
            var newPool = poolParent.AddComponent<Pool>();
            
            newPool.Setup(prefab, poolParent.transform);

            AllPools.Add(newPool);

            return newPool;
        }
        
        /// <summary>
        /// Default spawn method
        /// </summary>
        /// <returns> Spawned GameObject </returns>
        private static GameObject DefaultSpawn(GameObject prefab, Vector3 position, Quaternion rotation, 
            Transform parent, bool worldPositionStays)
        {
            if (IsEditor)
                return default;
            
            var pool = GetPoolByPrefab(prefab);
            var freePoolable = pool.GetFreeObject();
            var gameObject = freePoolable.gameObject;
            
            gameObject.SetActive(true);
            
            SetupTransform(freePoolable.transform, position, rotation, parent, worldPositionStays);
            
            CheckForSpawnEvents(gameObject);
            
            return gameObject;
        }
        
        /// <summary>
        /// Default despawn method
        /// </summary>
        /// <param name="toDespawn"> GameObject to despawn </param>
        /// <param name="delay"> For despawn with a delay </param>
        private static async void DefaultDespawn(GameObject toDespawn, float delay = 0f)
        {
            if (IsEditor)
                return;

            if (toDespawn.TryGetComponent(out Poolable poolable))
            {
                if (delay > 0)
                {
                    await Delay(delay);
                    
                    if (IsEditor)
                        return;
                    
                    if (toDespawn == null)
                        return;
                }
                
                var pool = poolable.Pool;

                if (pool != null)
                {
                    toDespawn.SetActive(false);
                    toDespawn.transform.SetParent(pool.PoolablesParent);
                
                    pool.IncludePoolable(poolable);
                }
                else
                {
                    Object.Destroy(toDespawn);
                }

                CheckForDespawnEvents(toDespawn);
            }
            else
            {
#if DEBUG
                Debug.LogError($"{toDespawn.name} was not spawned by NightPool and will be destroyed!");
#endif
                Object.Destroy(toDespawn, delay);
            }
        }
        
        /// <summary>
        /// Sets the position and rotation of Transform
        /// </summary>
        private static void SetupTransform(Transform transform, Vector3 position, Quaternion rotation, 
            Transform parent = null, bool worldPositionStays = false)
        {
            transform.SetParent(parent, worldPositionStays);
            transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>
        /// Tries to find IPoolItem component on GameObject and invokes void OnSpawn() if found
        /// </summary>
        private static void CheckForSpawnEvents(GameObject toCheck)
        {
            OnObjectSpawned?.Invoke(toCheck);

            toCheck.GetComponentsInChildren(ItemEventComponents);

            for (var i = 0; i < ItemEventComponents.Count; i++)
            {
                ItemEventComponents[i].OnSpawn();
            }
        }
        
        /// <summary>
        /// Tries to find IPoolItem component on GameObject and invokes void OnDespawn() if found
        /// </summary>
        private static void CheckForDespawnEvents(GameObject toCheck)
        {
            OnObjectDespawned?.Invoke(toCheck);
            
            toCheck.GetComponentsInChildren(ItemEventComponents);
            
            for (var i = 0; i < ItemEventComponents.Count; i++)
            {
                ItemEventComponents[i].OnDespawn();
            }
        }
        
        /// <summary>
        /// Resets static Lists in NightPool
        /// </summary>
        private static void ResetLists()
        {
            AllPools?.Clear();
            ItemEventComponents?.Clear();
        }

        /// <summary>
        /// Resets static Actions in NightPool
        /// </summary>
        private static void ResetActions()
        {
            OnObjectSpawned = null;
            OnObjectDespawned = null;
        }
    }
}