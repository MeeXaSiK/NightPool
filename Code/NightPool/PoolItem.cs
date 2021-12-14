// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021 Night Train Code
// ----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace NTC.Global.Pool
{
    [Serializable]
    public sealed class PoolItem
    {
        [SerializeField] private string name;

        [Space] public GameObject prefab;
                public int size;
        
        public string Tag => prefab.name;

        public PoolItem(GameObject go)
        {
            prefab = go;
        }
    }
}