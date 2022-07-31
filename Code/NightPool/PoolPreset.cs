// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021-2022 Night Train Code
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace NTC.Global.Pool
{
    [CreateAssetMenu(menuName = "Source/Pool/PoolPreset", fileName = "PoolPreset", order = 0)]
    public class PoolPreset : ScriptableObject
    {
        [SerializeField] private string poolName;
        [SerializeField] private List<PoolItem> poolItems = new List<PoolItem>(256);

        public IReadOnlyList<PoolItem> PoolItems => poolItems;

        public string GetName()
        {
            return poolName;
        }
    }
}