// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021-2022 Night Train Code
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace NTC.Pool
{
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = Constants.PoolsPresetComponentPath, fileName = "Pools Preset", order = 0)]
#endif
    public sealed class PoolsPreset : ScriptableObject
    {
        [SerializeField] private List<PoolPreset> _poolPresets = new List<PoolPreset>(256);

        public IReadOnlyList<PoolPreset> Presets => _poolPresets;
    }
}