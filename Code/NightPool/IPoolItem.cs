// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021-2022 Night Train Code
// ----------------------------------------------------------------------------

namespace NTC.Global.Pool
{
    public interface IPoolItem
    {
        public void OnSpawn();
        public void OnDespawn();
    }
}