// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021-2022 Night Train Code
// ----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace NTC.Global.Pool
{
    [DisallowMultipleComponent]
    public class Poolable : MonoBehaviour, IPoolItem
    {
        public Pool Pool { get; private set; }
        public GameObject Prefab { get; private set; }
        public bool IsActive { get; private set; }
        
        private bool isSetup;

        public void Setup(Pool pool, GameObject prefab, bool active)
        {
            if (isSetup)
                return;
            
            if (prefab == null)
                throw new NullReferenceException(nameof(prefab), null);

            gameObject.SetActive(active);

            IsActive = active;
            Pool = pool;
            Prefab = prefab;

            isSetup = true;
        }

        void IPoolItem.OnSpawn()
        {
            IsActive = true;
        }

        void IPoolItem.OnDespawn()
        {
            IsActive = false;
        }

        private void OnDestroy()
        {
            if (Pool != null)
            {
                Pool.ExcludePoolable(this);
            }
        }
    }
}