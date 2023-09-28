// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021-2023 Night Train Code
// ----------------------------------------------------------------------------

using System;
using UnityEngine;
using Object = UnityEngine.Object;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace NTC.Pool
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    internal sealed class Poolable
    {
        internal NightGameObjectPool _pool;
        internal Transform _transform;
        internal GameObject _gameObject;
        internal PoolableStatus _status;
        internal bool _isSetup;
        
        internal void SetupAsDefault()
        {
#if DEBUG
            if (_isSetup)
                throw new Exception("Poolable is already setup!");
#endif
            NightPool.ClonesMap.Add(_gameObject, this);
            _status = PoolableStatus.Despawned;
            _isSetup = true;
        }
        
        internal void SetupAsSpawnedOverCapacity()
        {
#if DEBUG
            if (_isSetup)
                throw new Exception("Poolable is already setup!");
#endif
            NightPool.ClonesMap.Add(_gameObject, this);
            _status = PoolableStatus.SpawnedOverCapacity;
            _isSetup = true;
        }

        internal void Dispose(bool immediately)
        {
            NightPool.ClonesMap.Remove(_gameObject);
            
            if (immediately)
                Object.DestroyImmediate(_gameObject);
            else
                Object.Destroy(_gameObject);
        }
    }
}