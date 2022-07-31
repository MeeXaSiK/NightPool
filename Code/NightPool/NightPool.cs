// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021-2022 Night Train Code
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NTC.Global.System;
using UnityEngine;
using Object = UnityEngine.Object;
using static NTC.Global.System.Tasks.TaskSugar;

namespace NTC.Global.Pool
{
    public static class NightPool
    {
        private static readonly Dictionary<GameObject, Queue<GameObject>> PoolDictionary =
            new Dictionary<GameObject, Queue<GameObject>>(64);
        
        private static readonly Dictionary<GameObject, Transform> ParentDictionary =
            new Dictionary<GameObject, Transform>(64);

        private static readonly List<IPoolItem> ItemEventComponents = 
            new List<IPoolItem>(32);

        private const int DefaultPoolCapacity = 64;

        public static event Action<GameObject> OnObjectSpawned;
        public static event Action<GameObject> OnObjectDespawned;

        private static bool IsEditor => !Application.isPlaying;

        public static void InstallPoolItems(PoolPreset poolPreset)
        {
            if (poolPreset == null || IsEditor)
                return;
            
            foreach (var poolItem in poolPreset.PoolItems)
            {
                var prefab = poolItem.Prefab;
                var isPoolExists = PoolDictionary.ContainsKey(prefab);
                var pool = isPoolExists 
                    ? PoolDictionary[prefab] 
                    : new Queue<GameObject>(DefaultPoolCapacity);

                for (var i = 0; i < poolItem.Size; i++)
                {
                    InstantiateIntoExistingPool(pool, poolItem.Prefab);
                }

                if (isPoolExists == false)
                {
                    PoolDictionary.Add(prefab, pool);
                }
            }
        }

        public static T Spawn<T>(T component, Vector3 position = default, Quaternion rotation = default) where T : Component
        {
            return 
                DefaultSpawn(component.gameObject, position, rotation, null, false).
                GetComponent<T>();
        }
        
        public static T Spawn<T>(T component, Transform parent, Quaternion rotation = default, 
            bool worldStaysPosition = false) where T : Component
        {
            var position = parent != null ? parent.position : Vector3.zero;
            
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
            var position = parent != null ? parent.position : Vector3.zero;
            
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

        public static async void DespawnAllThese(GameObject toDespawn, float delay = 0f)
        {
            if (toDespawn.TryGetComponent(out Poolable poolable))
            {
                var pool = PoolDictionary[poolable.Prefab];

                if (delay > 0)
                {
                    await Delay(delay);
                    if (IsEditor) return;
                }

                foreach (var item in pool)
                {
                    DefaultDespawn(item);
                }
            }
            else
            {
                Debug.LogError($"{toDespawn.name} was not spawned from pool!");
                
                Object.Destroy(toDespawn);
            }
        }

        public static async void DespawnAll(float delay = 0f)
        {
            if (delay > 0)
            {
                await Delay(delay);
                if (IsEditor) return;
            }
            
            foreach (var part in PoolDictionary)
            {
                var pool = part.Value;

                foreach (var item in pool)
                {
                    DefaultDespawn(item);
                }
            }
        }

        public static void DestroyPoolByGameObject(GameObject toDestroy)
        {
            if (toDestroy.TryGetComponent(out Poolable poolable))
            {
                if (PoolDictionary.TryGetValue(poolable.Prefab, out var pool))
                {
                    foreach (var gameObject in pool)
                    {
                        Object.Destroy(gameObject);
                    }

                    PoolDictionary.Remove(poolable.Prefab);
                }

                if (ParentDictionary.TryGetValue(poolable.Prefab, out var parent))
                {
                    Object.Destroy(parent.gameObject);

                    ParentDictionary.Remove(poolable.Prefab);
                }
            }
            else
            {
                Debug.LogError($"{toDestroy.name} was not spawned from pool!");
            }
        }

        public static void DestroyAllPools()
        {
            foreach (var pool in PoolDictionary.Values)
            {
                foreach (var gameObject in pool)
                {
                    Object.Destroy(gameObject);
                }
            }

            foreach (var parent in ParentDictionary.Values)
            {
                Object.Destroy(parent.gameObject);
            }
            
            PoolDictionary.Clear();
            ParentDictionary.Clear();
        }
        
        public static void Reset()
        {
            ResetLists();
            
            ResetActions();
        }

        private static GameObject DefaultSpawn(GameObject prefab, Vector3 position, Quaternion rotation, 
            Transform parent, bool worldPositionStays)
        {
            if (IsEditor)
                return default;
            
            var isPoolExists = PoolDictionary.ContainsKey(prefab);

            if (isPoolExists == false)
            { 
                return InstantiateGameObjectWithNewPool(prefab, position, rotation, parent, worldPositionStays);
            }

            var newObject = GetDisabledObjectFromPool(prefab);
            
            SetupTransform(newObject.transform, position, rotation, parent, worldPositionStays);
            
            CheckForSpawnEvents(newObject);
            
            return newObject;
        }
        
        private static async void DefaultDespawn(GameObject toDespawn, float delay = 0f)
        {
            if (IsEditor) return;

            if (toDespawn.TryGetComponent(out Poolable poolable))
            {
                var isPoolExists = PoolDictionary.ContainsKey(poolable.Prefab);

                if (delay > 0)
                {
                    await Delay(delay);
                
                    if (toDespawn == null || IsEditor)
                        return;
                }
            
                toDespawn.SetActive(false);
                toDespawn.transform.SetParent(GetPoolParent(poolable.Prefab));
            
                if (isPoolExists == false)
                {
                    var newPool = new Queue<GameObject>(DefaultPoolCapacity);
            
                    newPool.Enqueue(toDespawn);
                    PoolDictionary.Add(poolable.Prefab, newPool);
                }
            
                CheckForDespawnEvents(toDespawn);
            }
            else
            {
                Debug.LogError($"{toDespawn.name} was not spawned from pool!");
                
                Object.Destroy(toDespawn);
            }
        }
        
        private static void SetupTransform(Transform transform, Vector3 position, Quaternion rotation, 
            Transform parent = null, bool worldPositionStays = false)
        {
            if (parent != null)
            {
                transform.SetParent(parent, worldPositionStays);
            }
            
            transform.SetPositionAndRotation(position, rotation);
        }
        
        private static void CheckForSpawnEvents(GameObject toCheck)
        {
            OnObjectSpawned?.Invoke(toCheck);

            toCheck.GetComponentsInChildren(ItemEventComponents);

            for (var i = 0; i < ItemEventComponents.Count; i++)
            {
                ItemEventComponents[i]?.OnSpawn();
            }
        }
        
        private static void CheckForDespawnEvents(GameObject toCheck)
        {
            OnObjectDespawned?.Invoke(toCheck);
            
            toCheck.GetComponentsInChildren(ItemEventComponents);
            
            for (var i = 0; i < ItemEventComponents.Count; i++)
            {
                ItemEventComponents[i]?.OnDespawn();
            }
        }

        private static GameObject GetDisabledObjectFromPool(GameObject prefab, bool active = true)
        {
            var pool = PoolDictionary[prefab];

            foreach (var freeObject in pool)
            {
                if (freeObject == null)
                    continue;
                
                if (freeObject.activeInHierarchy)
                    continue;
                
                pool.Enqueue(freeObject);
                freeObject.SetActive(active);
                
                return freeObject;
            }
            
            return InstantiateIntoExistingPool(pool, prefab, active);
        }
        
        private static GameObject InstantiateGameObjectWithNewPool(GameObject prefab, Vector3 position, 
            Quaternion rotation, Transform parent = null, bool worldPositionStays = false)
        {
            var newPool = new Queue<GameObject>(DefaultPoolCapacity);
            var newObject = InstantiateIntoExistingPool(newPool, prefab, true);
            
            SetupTransform(newObject.transform, position, rotation, parent, worldPositionStays);

            PoolDictionary.Add(prefab, newPool);
            CheckForSpawnEvents(newObject);
                
            return newObject;
        }
        
        private static GameObject InstantiateIntoExistingPool(
            Queue<GameObject> pool, GameObject prefab, bool active = false)
        {
            var prefabName = prefab.name;
            var poolParent = GetPoolParent(prefab);
            var newObject = Object.Instantiate(prefab, poolParent);
                    
            newObject.name = prefabName;
            newObject.SetActive(active);
            pool.Enqueue(newObject);

            if (newObject.GetComponent<Poolable>() == false)
            {
                var poolable = newObject.AddComponent<Poolable>();
                
                poolable.Setup(prefab, poolParent);
            }

            return newObject;
        }
        
        private static Transform GetPoolParent(GameObject prefab)
        {
            return ParentDictionary.ContainsKey(prefab) 
                ? ParentDictionary[prefab] 
                : NewPoolParent(prefab);
        }

        private static Transform NewPoolParent(GameObject prefab)
        {
            var poolParent =
                new GameObject($"[NightPool] {prefab.name}").transform;
            
            ParentDictionary.Add(prefab, poolParent);
            
            return poolParent;
        }
        
        private static void ResetLists()
        {
            ItemEventComponents?.Clear();
            PoolDictionary?.Clear();
            ParentDictionary?.Clear();
        }

        private static void ResetActions()
        {
            OnObjectSpawned?.SetNull();
            OnObjectDespawned?.SetNull();
        }
    }
}