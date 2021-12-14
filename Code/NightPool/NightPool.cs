// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021 Night Train Code
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
        private static readonly Dictionary<string, Queue<GameObject>> PoolDictionary =
            new Dictionary<string, Queue<GameObject>>(64);
        
        private static readonly Dictionary<string, Transform> ParentDictionary =
            new Dictionary<string, Transform>(64);

        private static readonly List<IPoolItem> ItemEventComponents = new List<IPoolItem>(32);
        private static readonly int DefaultPoolCapacity = 64;
        
        public static event Action<GameObject> OnObjectSpawned;
        public static event Action<GameObject> OnObjectDespawned;

        private static bool IsEditor => !Application.isPlaying;

        public static void InstallPoolItems(PoolPreset poolPreset)
        {
            if (poolPreset == null || IsEditor)
                return;
            
            foreach (var poolItem in poolPreset.poolItems)
            {
                var poolItemTag = poolItem.Tag;
                var isPoolExists = PoolDictionary.ContainsKey(poolItemTag);
                var newPool = isPoolExists 
                    ? PoolDictionary[poolItemTag] 
                    : new Queue<GameObject>(DefaultPoolCapacity);

                for (var i = 0; i < poolItem.size; i++)
                {
                    InstantiateIntoExistingPool(newPool, poolItem.prefab, poolItemTag);
                }

                if (isPoolExists == false)
                {
                    PoolDictionary.Add(poolItemTag, newPool);
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
            var prefabName = toDespawn.name;
            var pool = PoolDictionary[prefabName];

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
        
        public static void Reset()
        {
            ResetLists();
            ResetActions();
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
        
        private static GameObject DefaultSpawn(GameObject toSpawn, Vector3 position, Quaternion rotation, 
            Transform parent, bool worldPositionStays)
        {
            if (IsEditor)
                return default;
            
            var prefabName = toSpawn.name;
            var isPoolExists = PoolDictionary.ContainsKey(prefabName);

            if (isPoolExists == false)
            { 
                return InstantiateGameObjectWithNewPool(
                    toSpawn, prefabName, position, rotation, parent, worldPositionStays);
            }

            var newObject = GetDisabledObjectFromPool(toSpawn, prefabName);
            
            SetupTransform(newObject.transform, position, rotation, parent, worldPositionStays);
            CheckForSpawnEvents(newObject);
            
            return newObject;
        }
        
        private static async void DefaultDespawn(GameObject toDespawn, float delay = 0f)
        {
            if (IsEditor) return;
            
            var prefabName = toDespawn.name;
            var isPoolExists = PoolDictionary.ContainsKey(prefabName);

            if (delay > 0)
            {
                await Delay(delay);
                
                if (toDespawn == null || IsEditor)
                    return;
            }
            
            toDespawn.SetActive(false);
            toDespawn.transform.SetParent(GetPoolParent(prefabName));
            
            if (isPoolExists == false)
            {
                var newPool = new Queue<GameObject>(DefaultPoolCapacity);
            
                newPool.Enqueue(toDespawn);
                PoolDictionary.Add(prefabName, newPool);
            }
            
            CheckForDespawnEvents(toDespawn);
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

        private static GameObject InstantiateGameObjectWithNewPool(GameObject toSpawn, string name, Vector3 position, 
            Quaternion rotation, Transform parent = null, bool worldPositionStays = false)
        {
            var newPool = new Queue<GameObject>(DefaultPoolCapacity);
            var newPoolItemObject = InstantiateIntoExistingPool(newPool, toSpawn, name, true);
            
            SetupTransform(newPoolItemObject.transform, position, rotation, parent, worldPositionStays);

            PoolDictionary.Add(name, newPool);
            CheckForSpawnEvents(toSpawn);
                
            return newPoolItemObject;
        }

        private static GameObject GetDisabledObjectFromPool(GameObject toSpawn, string prefabName, bool active = true)
        {
            var pool = PoolDictionary[prefabName];

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
            
            return InstantiateIntoExistingPool(pool, toSpawn, prefabName, active);
        }
        
        private static GameObject InstantiateIntoExistingPool(
            Queue<GameObject> pool, GameObject toSpawn, string prefabName, bool active = false)
        {
            var newObject = Object.Instantiate(toSpawn, GetPoolParent(prefabName));
                    
            newObject.name = prefabName;
            newObject.SetActive(active);
            pool.Enqueue(newObject);

            return newObject;
        }
        
        private static Transform GetPoolParent(string prefabName)
        {
            return ParentDictionary.ContainsKey(prefabName) 
                ? ParentDictionary[prefabName] 
                : NewPoolParent(prefabName);
        }

        private static Transform NewPoolParent(string prefabName)
        {
            var poolParent =
                new GameObject($"[NightPool] {prefabName}").transform;
            
            ParentDictionary.Add(prefabName, poolParent);
            
            return poolParent;
        }
    }
}