# NightPool

* Spawn objects fast! Fast Game Object Pool for Unity by [**Night Train Code**](https://www.youtube.com/c/NightTrainCode/)

# Navigation

* [Main](#nightpool)
* [Installation](#how-to-use)
* [NightPool 1.3 [Update]](#whats-new-in-nightpool-13)

# How to use

> YouTube Video about NightPool

[![NightCache YouTube Video](https://img.youtube.com/vi/YPWriGuO72Q/0.jpg)](https://www.youtube.com/watch?v=YPWriGuO72Q)

1) Install `NightPool` into your Unity project

2) Replace all `Instantiate()` and `Destroy()` methods with `NightPool.Spawn()` or `NightPool.Despawn()`

## For Example

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

3) For pre-cache objects on awake, create PoolPreset (Create -> Source -> Pool -> PoolPreset)

4) Add objects for pool in list:

| Parameters | Info |
| ------ | ------ |
| `Name` | Created for convenience and does not affect anything |
| `Prefab` | Object to spawn |
| `Size` | Spawn count |

5) Add component `NightPoolEntry` on any `GameObject` in scene and set created `PoolPreset`

6) If you want to invoke methods `OnSpawn()` and `OnDespawn()`, set bool `CheckForEvents` true in `NightPool.Spawn()` or `NightPool.Despawn()` arguments and implement interface `IPoolItem` on GameObject

```csharp
    public class GameObjectSpawner : MonoBehaviour
    {
        public Unit unit;
    
        private void SpawnUnit()
        {
            Unit spawnedUnit = NightPool.Spawn(unit, transform.position, Quaternion.identity, true);
        }
        
        private void DespawnUnit()
        {
            NightPool.Despawn(unit, true);
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

# What's new in NightPool 1.3

* Old

```csharp
    public class Demo : MonoBehaviour
    {
        [SerializeField] private Unit unitPrefab;
    
        private void Start()
        {
            Unit newUnit = NightPool.Spawn(unitPrefab).GetComponent<Unit>();
        }
    }
```

* New

```csharp
    public class Demo : MonoBehaviour
    {
        [SerializeField] private Unit unitPrefab;
    
        private void Start()
        {
            Unit newUnit = NightPool.Spawn(unitPrefab);
        }
    }
```
