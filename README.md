# NightPool
* Fast Game Object Pool for Unity by [**Night Train Code**](https://www.youtube.com/c/NightTrainCode/)
* Spawn objects fast!
* Awesome performance
* Easy to use

## Navigation

* [Main](#nightpool)
* [Installation](#installation)
* [How to use](#how-to-use)
* [Game Object pre-caching](#game-object-pre-caching)
* [OnSpawn & OnDespawn events](#ipoolitem)

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
    Destroy(_spawnedGameObject);
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
    NightPool.Despawn(_spawnedGameObject);
}
```

## Game Object pre-caching

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
