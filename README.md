# NightPool
* Fast Game Object Pool for Unity by [**Night Train Code**](https://www.youtube.com/c/NightTrainCode/)
* Spawn objects fast!
* Awesome performance
* Easy to use

## Navigation

* [Main](#nightpool)
* [Installation](#installation)
* [How to use](#how-to-use)
* [Game Objects pre-caching](#game-objects-pre-caching)
* [OnSpawn & OnDespawn events](#ipoolitem)
* [NightPoolDespawner](#night-pool-despawner)
* [Other features](#other-features)

## Installation

1. Install `NightPool` in your Unity project
2. Add component `NightPoolEntry` on any `GameObject` in scene

## How to use

Replace `Instantiate()` and `Destroy()` methods with `NightPool.Spawn()` or `NightPool.Despawn()` in your code.

`Old variant:`

```csharp
private void Spawn()
{
    _spawnedGameObject = Instantiate(prefab, transform.position, Quaternion.identity);
}
        
private void Despawn()
{
    Destroy(_spawnedGameObject, 3f);
}
```

`New variant:`

```csharp
private void Spawn()
{
    _spawnedGameObject = NightPool.Spawn(prefab, transform.position, Quaternion.identity);
}
        
private void Despawn()
{
    NightPool.Despawn(_spawnedGameObject, 3f);
}
```

## Game Objects pre-caching

To pre-cache objects on `Awake()` you can create PoolPreset `Create -> Source -> Pool -> PoolPreset`

And add objects for pool in list:

| Parameters | Info |
| ------ | ------ |
| `Prefab` | Object to spawn |
| `Size` | Spawn count |

At the end set created `PoolPreset` in `NightPoolEntry`

## IPoolItem

If you want to invoke methods `OnSpawn()` and `OnDespawn()` on poolable GameObject, implement interface `IPoolItem` on GameObject

```csharp
    public class Unit : MonoBehaviour, IPoolItem
    {
        void IPoolItem.OnSpawn()
        {
            Debug.Log("Unit Spawned");
        }
        
        void IPoolItem.OnDespawn()
        {
            Debug.Log("Unit Despawned");
        }
    }
```

## Night Pool Despawner

You can add component `NightPoolDespawner` on any prefab that will be spawned and must be despawned after a certain amount of time. This time is set in the field `TimeToDespawn`.

## Other features

**`NightPool`**

| Method or Event | Info |
| ------ | ------ |
| `Action<GameObject> OnObjectSpawned` | This `Action` called on any object spawned |
| `Action<GameObject> OnObjectDespawned` | This `Action` called on any object despawned |
| `NightPool.InstallPoolItems()` | Pre-caches game objects by `PoolPreset` |
| `NightPool.DestroyPool(gameObject)` | Destroys pool by `GameObject` or `Pool`|
| `NightPool.DestroyAllPools()` | Destroys all pools |
| `NightPool.GetPoolByPrefab()` | Returns `Pool` by prefab |
| `NightPool.Reset()` | Resets the `NightPool`. Called in `NightPoolEntry` in `OnDestroy` |

**`Poolable`** added to any object created using the NightPool

| Property | Info |
| ------ | ------ |
| `Pool` | Pool of the current poolable object |
| `Prefab` | Prefab of the current poolable object |
| `IsActive` | Means the object is enabled or disabled |

**`Pool`** creates for each prefab that was used to spawn object

| Property or Method | Info |
| ------ | ------ |
| `Prefab` | Prefab of the current pool |
| `PoolablesParent` | Parent transform for each spawned game object of the current pool |
| `Poolables` | All spawned poolables of the current Pool |
| `GetFreeObject()` | Returns unused poolable object |
| `PopulatePool()` | Populates pool by spawn new poolable objects |
| `IncludePoolable()` | Includes new poolable objects in pool |
| `ExcludePoolable()` | Excludes existing spawned objects from pool |
