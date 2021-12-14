# NightPool

* Spawn objects fast! Fast Game Object Pool for Unity by [**Night Train Code**](https://www.youtube.com/c/NightTrainCode/)

# Navigation

* [Main](#nightpool)
* [Installation](#how-to-use)

# How to use

> YouTube Video about NightPool

[![NightCache YouTube Video](https://img.youtube.com/vi/YPWriGuO72Q/0.jpg)](https://www.youtube.com/watch?v=YPWriGuO72Q)

1) Install `NightPool` into your Unity project

2) Add component `NightPoolEntry` on any `GameObject` in scene

3) Replace all `Instantiate()` and `Destroy()` methods with `NightPool.Spawn()` or `NightPool.Despawn()`

### Old:

```csharp
    public class GameObjectSpawner : MonoBehaviour
    {
        private void Spawn()
        {
            Instantiate(gameObject, transform.position, Quaternion.identity);
        }
        
        private void Despawn()
        {
            Destroy(gameObject);
        }
    }
```

### New:

```csharp
    public class GameObjectSpawner : MonoBehaviour
    {
        private void Spawn()
        {
            NightPool.Spawn(gameObject, transform.position, Quaternion.identity);
        }
        
        private void Despawn()
        {
            NightPool.Despawn(gameObject);
        }
    }
```

4) For pre-cache objects on awake, create PoolPreset (Create -> Source -> Pool -> PoolPreset)

5) Add objects for pool in list:

| Parameters | Info |
| ------ | ------ |
| `Name` | Created for convenience and does not affect anything |
| `Prefab` | Object to spawn |
| `Size` | Spawn count |

6) Set created `PoolPreset` in `NightPoolEntry`

7) If you want to invoke methods `OnSpawn()` and `OnDespawn()`, implement interface `IPoolItem` on GameObject

```csharp
    public class GameObjectSpawner : MonoBehaviour
    {
        public Unit unit;
    
        private void SpawnUnit()
        {
            Unit spawnedUnit = NightPool.Spawn(unit, transform.position, Quaternion.identity);
        }
        
        private void DespawnUnit()
        {
            NightPool.Despawn(unit);
        }
    }
```

```csharp
    public class Unit : MonoBehaviour, IPoolItem
    {
        public void OnSpawn()
        {
            //DoSomething
        }
        
        public void OnDespawn()
        {
            Unit.UnitHealth.Health = 100;
        }
    }
```
