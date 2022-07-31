// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021-2022 Night Train Code
// ----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace NTC.Global.Pool
{
    [Serializable]
    public sealed class PoolItem
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int size;
        
        public GameObject Prefab => prefab;
        public int Size => size;
    }
}