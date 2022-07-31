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
    public class Poolable : MonoBehaviour
    {
        public GameObject Prefab { get; private set; }
        public Transform PoolParent { get; private set; }

        private bool isSetup;

        public void Setup(GameObject prefab, Transform poolParent)
        {
            if (isSetup)
                return;
            
            if (prefab == null)
                throw new NullReferenceException(nameof(prefab), null);
            
            if (poolParent == null)
                throw new NullReferenceException(nameof(poolParent), null);

            Prefab = prefab;
            PoolParent = poolParent;

            isSetup = true;
        }
    }
}