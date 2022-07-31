# NightPool

* Spawn objects fast! Fast Game Object Pool for Unity by [**Night Train Code**](https://www.youtube.com/c/NightTrainCode/)

# Navigation

* [Main](#nightpool)
* [Installation](#how-to-use)

# How to use

> YouTube Video about NightPool (old version)

[![NightCache YouTube Video](https://img.youtube.com/vi/YPWriGuO72Q/0.jpg)](https://www.youtube.com/watch?v=YPWriGuO72Q)

1) Install `NightPool` into your Unity project

2) Add component `NightPoolEntry` on any `GameObject` in scene

3) Replace all `Instantiate()` and `Destroy()` methods with `NightPool.Spawn()` or `NightPool.Despawn()`

### Old:

```csharp
    public class GameObjectSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
    
        private GameObject _spawnedGameObject;
    
        private void Spawn()
        {
            _spawnedGameObject = Instantiate(prefab, transform.position, Quaternion.identity);
        }
        
        private void Despawn()
        {
            Destroy(_spawnedGameObject);
        }
    }
```

### New:

```csharp
    public class GameObjectSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
    
        private GameObject _spawnedGameObject;
    
        private void Spawn()
        {
            _spawnedGameObject = NightPool.Spawn(prefab, transform.position, Quaternion.identity);
        }
        
        private void Despawn()
        {
            NightPool.Despawn(_spawnedGameObject);
        }
    }
```

4) For pre-cache objects on awake, create PoolPreset (Create -> Source -> Pool -> PoolPreset)

5) Add objects for pool in list:

| Parameters | Info |
| ------ | ------ |
| `Prefab` | Object to spawn |
| `Size` | Spawn count |

6) Set created `PoolPreset` in `NightPoolEntry`

7) If you want to invoke methods `OnSpawn()` and `OnDespawn()`, implement interface `IPoolItem` on GameObject

```csharp
    public class UnitSpawner : MonoBehaviour
    {
        [SerializeField] private Unit unitPrefab;
        
        private Unit _spawnedUnit;
    
        private void SpawnUnit()
        {
            _spawnedUnit = NightPool.Spawn(unitPrefab, transform.position, Quaternion.identity);
        }
        
        private void DespawnUnit()
        {
            NightPool.Despawn(_spawnedUnit);
        }
    }
```

```csharp
    public class Unit : MonoBehaviour, IPoolItem
    {
        void IPoolItem.OnSpawn()
        {
            //DoSomething
        }
        
        void IPoolItem.OnDespawn()
        {
            //DoSomething
        }
    }
```
