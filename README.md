# 🚄 Night Pool
[![License](https://img.shields.io/github/license/meexasik/nightpool?color=318CE7&style=flat-square)](LICENSE.md) [![Version](https://img.shields.io/github/package-json/v/MeeXaSiK/NightPool?color=318CE7&style=flat-square)](package.json) [![Unity](https://img.shields.io/badge/Unity-2020.3+-2296F3.svg?color=318CE7&style=flat-square)](https://unity.com/)

**Night Pool** is a fast and lightweight game object pooling solution for Unity by [**Night Train Code**](https://www.youtube.com/c/NightTrainCode/)
* 🚀 Improves performance
* 😃 Allows you to cache game objects for future use
* ✅ Supports functionality of `Instantiate` and `Destroy` methods
* 🔄 Supports recycling of game objects if pools overflow
* 😋 Easily to attach your DI solution
* 🗑 Reduces GC allocations
> **Warning!** All internal checks and exceptions works only in `DEBUG` build versions to improve performance. Enable `Development Build` option in **Build Settings** for development builds and disable for releases!

🚘 **Old default variant:**
```csharp
var newGameObject = Object.Instantiate(_prefab);

Object.Destroy(newGameObject);
```
🚀 **New performant variant:**
```csharp
var newGameObject = NightPool.Spawn(_prefab);

NightPool.Despawn(newGameObject);
```

# 🌐 Navigation

* [Main](#-night-pool)
* [Installation](#-installation)
   * [As a Unity module](#as-a-unity-module)
   * [As source](#as-source)
* [Initial setup](#-initial-setup)
* [How to use](#-how-to-use)
   * [Basics](#basics)
   * [Overloads](#overloads)
   * [Delayed despawn](#delayed-despawn)
* [Components](#-components)
   * [Night Pool Global](#night-pool-global)
   * [Pools Preset](#pools-preset)
   * [Night Game Object Pool](#night-game-object-pool)
   * [Other Night Pool static methods](#other-night-pool-static-methods)
   * [Night Pool Despawn Timer](#night-pool-despawn-timer)
* [Callbacks](#-callbacks)
   * [ISpawnable](#ispawnable)
   * [IDespawnable](#idespawnable)
   * [IPoolable](#ipoolable)
* [Events](#-events)
   * [Game Object Instantiated](#game-object-instantiated)
   * [Game Object Spawned](#game-object-spawned)
   * [Game Object Despawned](#game-object-despawned)
* [Enums](#-enums)
   * [Night Pool Mode](#night-pool-mode)
   * [Behaviour On Capacity Reached](#behaviour-on-capacity-reached)
   * [Despawn Type](#despawn-type)
   * [Preload Type](#preload-type)
   * [Callbacks Type](#callbacks-type)
   * [Update Type](#update-type)
   * [Reaction On Repeated Delayed Despawn](#reaction-on-repeated-delayed-despawn)
* [Extensions](#-extensions)
   * [Particle System Auto Despawn](#particle-system-auto-despawn)
* [Preload In Editor](#-preload-in-editor)
* [How to attach DI container](#-how-to-attach-di-container)
* [How to reset pooled rigidbody](#-how-to-reset-pooled-rigidbody)

# ▶ Installation
## As a Unity module
Supports installation as a Unity module via a git link in the **PackageManager**
```
https://github.com/MeeXaSiK/NightPool.git
```
or direct editing of `Packages/manifest.json`:
```
"com.nighttraincode.nightpool": "https://github.com/MeeXaSiK/NightPool.git",
```
## As source
You can also clone the code into your Unity project.
# 🔸 Initial setup
Add the `Night Pool Global` component to any game object in scene.

**From the component menu:**

`Add Component -> Night Train Code -> Night Pool -> Night Pool Global`

**Or from project files:**

Find `NightPoolGlobal.cs` in the project files and drag it onto any game object.

> The Night Pool asset will work without manually adding the main component, but you will lose the ability to change the global settings.

# 🔸 How to use
## Basics
Use static class `NightPool` to `Spawn` or `Despawn` game objects instead of `Instantiate` and `Destroy`.
```csharp
using NTC.Pool;

public class Example : MonoBehaviour
{
    [SerializeField] private GameObject _gameObjectPrefab;
    [SerializeField] private TestComponent _componentPrefab;
    
    public void Foo()
    {
        // Spawns a GameObject clone by prefab.
        GameObject newGameObject = NightPool.Spawn(_gameObjectPrefab);
            
        // Spawns a TestComponent clone by prefab.
        TestComponent newTestComponent = NightPool.Spawn(_componentPrefab);
            
        // Despawns a GameObject clone.
        NightPool.Despawn(newGameObject);
            
        // Despawns a TestComponent clone.
        NightPool.Despawn(newTestComponent);
    }
}
```
## Overloads
`where T : Component or GameObject`
```csharp
public static T Spawn<T>(T prefab);
```
```csharp
public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation);
```
```csharp
public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent);
```
```csharp
public static T Spawn<T>(T prefab, Transform parent, bool worldPositionStays = false);
```
```csharp
public static T Despawn<T>(T clone, float delay = 0f);
```
## Delayed despawn
If you want to perform a despawn with a delay, you can pass the delay time to the arguments of the `Despawn` method:
```csharp
NightPool.Despawn(newGameObject, 5f);
```
> GameObject will be despawned after 5 seconds.

# 🔸 Components
## Night Pool Global
This component allows you to set global pool settings, preload pools and change safety options.

| Field | Info |
| ------ | ------ |
| [Update Type](#update-type) | Determines in which update method the processing should be performed. Delayed despawns are currently handled in the selected update loop of this component. |
| [Preload Pools Type](#preload-type) | Should the pools be preloaded? |
| [Pools Preset](#pools-preset) | Stores settings for preloading pools. |

**Global pool settings:**

| Field | Info |
| ------ | ------ |
| [Behaviour On Capacity Reached](#behaviour-on-capacity-reached) | What to do when pool overflows? |
| [Despawn Type](#despawn-type) | How clones of the pool's prefab must be despawned? |
| [Callbacks Type](#callbacks-type) | Whether to make callbacks when clones spawn and despawn? |
| Capacity | Default capacity for runtime created pools. |
| Dont Destroy On Load | Should pools created at runtime be persistent? |
| Send Warnings | Should pools created at runtime find issues and log warnings by default? |
> Global settings will be applied to new pools created at runtime.

**Safety:**
| Field | Info |
| ------ | ------ |
| [Night Pool Mode](#night-pool-mode) | Should Night Pool focus more on performance or total safety? |
| [Reaction On Repeated Delayed Despawn](#reaction-on-repeated-delayed-despawn) | What will be done if you try to destroy the same clone multiple times with a delay? |
| Despawn Persistent Clones On Destroy | Should persistent clones be despawned on destroy? |
| Check Clones For Null | Should clones be checked for null on spawn? |
| Check For Prefab | Should a pool prefab be checked during setup to see if it is really prefab? |
| Clear Events On Destroy | Should the `NightPool` static events be cleared on destroy? |

## Pools Preset
Stores settings for preloading pools.

`Create -> Night Train Code -> Night Pool -> Pools Preset`

| Field | Info |
| ------ | ------ |
| Name | It is here for convenience and beauty in the inspector and does not affect anything. The name of an element in the list will change if you change the name. |
| Prefab | The prefab that pool will control. |
| [Behaviour On Capacity Reached](#behaviour-on-capacity-reached) | What to do when pool overflows? |
| [Despawn Type](#despawn-type) | How clones of the prefab must be despawned? |
| [Callbacks Type](#callbacks-type) | Whether to make callbacks when clones spawn and despawn? |
| Capacity | Capacity of the pool. |
| Preload Size | Preload size of the pool. |
| Dont Destroy On Load | Should the pool be persistent? |
| Warnings | Should the pool find issues and log warnings by default? |

## Night Game Object Pool
This component allows you to set up a specific pool.

`Add Component -> Night Train Code -> Night Pool -> Night Game Object Pool`

### Fields
| Field | Info |
| ------ | ------ |
| Prefab | The prefab that pool will control. |
| [Behaviour On Capacity Reached](#behaviour-on-capacity-reached) | What to do when pool overflows? |
| [Despawn Type](#despawn-type) | How clones of the prefab must be despawned? |
| Capacity | Capacity of the pool. |
| [Preload Type](#preload-type) | Clones preload type of the pool. |
| Preload Size | Preload size of the pool. |
| [Callbacks Type](#callbacks-type) | Whether to make callbacks when clones spawn and despawn? |
| Dont Destroy On Load | Should the pool be persistent? |
| Send Warnings | Should the pool find issues and log warnings by default? |

### ReadOnly Fields

| ReadOnly Field | Info |
| ------ | ------ |
| All Clones Count | Number of all game objects in the pool. |
| Spawed Clones Count | Number of spawned game objects in the pool. |
| Despawned Clones Count | Number of despawned game objects in the pool. |
| Spawns Count | Total number of spawns in the pool. |
| Despawns Count | Total number of despawns in the pool. |
| Total | Total number of spawns and despawns in the pool. |
| Instantiated | Total number of instantiates in the pool. |
### Properties
```csharp
[SerializeField] private NightGameObjectPool _pool;

private void Foo()
{
    // Prefab attached to the pool.
    GameObject prefab = _pool.AttachedPrefab;

    // Overflow behaviour of the pool.
    BehaviourOnCapacityReached behaviourOnCapacityReached = _pool.BehaviourOnCapacityReached;

    // GameObject despawn type of the pool.
    DespawnType despawnType = _pool.DespawnType;

    // Callbacks type on GameObject spawned or despawned of the pool.
    CallbacksType callbacksType = _pool.CallbacksType;

    // Capacity of the pool.
    int capacity = _pool.Capacity;

    // Number of spawned clones.
    int spawnedClonesCount = _pool.SpawnedClonesCount;
            
    // Number of despawned clones.
    int despawnedClonesCount = _pool.DespawnedClonesCount;

    // Number of all clones.
    int allClonesCount = _pool.AllClonesCount;

    // Total number of spawns in the pool.
    int spawnsCount = _pool.SpawnsCount;
            
    // Total number of despawns in the pool.
    int despawnsCount = _pool.DespawnsCount;
            
    // Total number of instantiates in the pool.
    int instantiatesCount = _pool.InstantiatesCount;

    // Total number of spawns and despawns in the pool.
    int totalActionsCount = _pool.TotalActionsCount;

    // Has pool registered as persistent?
    bool hasPoolRegisteredAsPersistent = _pool.HasRegisteredAsPersistent;
}
```
### Methods

```csharp
[SerializeField] private NightGameObjectPool _pool;
[SerializeField] private GameObject _prefab;

private void Foo()
{
    // Initializes the pool manually. This may be necessary
    // if your Awake Method through which you access the pool
    // is called before the pool is initialized.
    _pool.Init();

    // This method also contains an overload for setting a prefab.
    _pool.Init(_prefab);

    // Populates the pool.
    _pool.PopulatePool(16);
            
    // Sets the pool capacity.
    _pool.SetCapacity(32);
            
    // Sets the pool overflow behaviour.
    _pool.SetBehaviourOnCapacityReached(BehaviourOnCapacityReached.Recycle);
            
    // Sets the pool GameObject despawn type.
    _pool.SetDespawnType(DespawnType.DeactivateAndHide);
            
    // Sets the pool callbacks type on GameObject spawned or despawned.
    _pool.SetCallbacksType(CallbacksType.Interfaces);
            
    // Sets the pool warnings active.
    _pool.SetWarningsActive(true);
            
    // Performs an action for each clone in the pool.
    _pool.ForEachClone(Debug.Log);
            
    // Performs an action for each spawned clone in the pool.
    _pool.ForEachSpawnedClone(Debug.Log);
            
    // Performs an action for each despawned clone in the pool.
    _pool.ForEachDespawnedClone(Debug.Log);
            
    // Destroys spawned clones.
    _pool.DestroySpawnedClones();
            
    // Destroys despawned clones.
    _pool.DestroyDespawnedClones();

    // Destroys all clones in the pool.
    _pool.DestroyAllClones();

    // Destroys spawned clones immediately.
    _pool.DestroySpawnedClonesImmediate();
            
    // Destroys despawned clones immediately.
    _pool.DestroyDespawnedClonesImmediate();

    // Destroys all clones in the pool immediately.
    _pool.DestroyAllClonesImmediate();
            
    // Destroys the pool.
    _pool.DestroyPool();
            
    // Destroys the pool immediately.
    _pool.DestroyPoolImmediate();
            
    // Despawns spawned clones in the pool.
    _pool.DespawnAllClones();

    // Clears the pool.
    _pool.Clear();
}
```
> **Warning!** When the pool is destroyed, the clones will also be destroyed!

## Other Night Pool static methods
Other `NightPool` static methods.
```csharp
[SerializeField] private PoolsPreset _poolsPreset;
[SerializeField] private TestComponent _prefab;
        
private NightGameObjectPool _pool;
        
private void Foo()
{
   // Installs pools using PoolsPreset.
   NightPool.InstallPools(_poolsPreset);
   
   // Spawns a TestComponent clone by prefab.
   TestComponent spawnedClone = NightPool.Spawn(_prefab);
   
   // Tries to get pool by clone.
   _pool = NightPool.GetPoolByClone(spawnedClone);
   
   // Tries to get pool by prefab.
   _pool = NightPool.GetPoolByPrefab(_prefab);

   // Performs an action for each pool.
   NightPool.ForEachPool(Debug.Log);

   // Performs an action for each clone.
   NightPool.ForEachClone(Debug.Log);
            
   // Tries to get pool by spawned clone.
   bool wasPoolFoundByClone = NightPool.TryGetPoolByClone(spawnedClone, out _pool);

   // Tries to get pool by prefab.
   bool wasPoolFoundByPrefab = NightPool.TryGetPoolByPrefab(_prefab, out _pool);

   // Tries to get status of the clone (Spawned / Despawned / SpawnedOverCapacity).
   PoolableStatus cloneStatus = NightPool.GetCloneStatus(spawnedClone);

   // Is the game object a clone (spawned using Night Pool)?
   bool isClone = NightPool.IsClone(spawnedClone);
            
   // Destroys the clone.
   // If you want to destroy the clone but not despawn one, use this method to avoid errors!
   NightPool.DestroyClone(spawnedClone);

   // Destroys the clone immediately.
   // If you want to destroy the clone immediately but not despawn one, use this method to avoid errors!
   NightPool.DestroyCloneImmediate(spawnedClone);
            
   // Destroys all pools.
   NightPool.DestroyAllPools(immediately: false);
}
```
> **Warning!** If you want to destroy the clone but not despawn one, use `NightPool.DestroyClone` to avoid errors!

> **Warning!** When the pool is destroyed, the clones will also be destroyed!

## Night Pool Despawn Timer
This component allows you to despawn a clone some time after it spawns.

`Add Component -> Night Train Code -> Night Pool -> Night Pool Despawn Timer`

| Field | Info |
| ------ | ------ |
| [Update Type](#update-type) | Determines in which update method the timer should be updated. |
| Time To Despawn | Time after which the game object will be despawned. |

> **Warning!** If the pool is recycling and it has callbacks disabled, then the timer may not work correctly! Enable callbacks or choose another [`Behaviour On Capacity Reached`](#behaviour-on-capacity-reached) of the pool instead of recycling to avoid erros.

# 🔸 Callbacks
You can implement various interfaces to perform callbacks. Also supports `SendMessage` and `BroadcastMessage`. You can set the required [`Callbacks Type`](#callbacks-type) in the [`Night Game Object Pool`](#night-game-object-pool).
## ISpawnable
This interface allows you to do something on game object spawn.
```csharp
public class Example : MonoBehaviour, ISpawnable
{
    public void OnSpawn()
    {
        // Do something on spawn.
    }
}
```
## IDespawnable
This interface allows you to do something on game object despawn.
```csharp
public class Example : MonoBehaviour, IDespawnable
{
    public void OnDespawn()
    {
        // Do something on despawn.
    }
}
```
## IPoolable
This interface inherits from `ISpawnable` and `IDespawnable` and allows you to do something on game object spawn and despawn.
```csharp
public class Example : MonoBehaviour, IPoolable
{
    public void OnSpawn()
    {
        // Do something on spawn.
    }
    
    public void OnDespawn()
    {
        // Do something on despawn.
    }
}
```
# 🔸 Events
> You can enable option `Clear Events On Destroy` in the `Night Pool Global` to automatically clear all static events. If you want to completely clear the event manually you can use `NightPoolEvent<T> event.Clear();` method.
## Game Object Instantiated
This static event is called when a game object has not yet been cached and spawns inside the pool using instantiate.
```csharp
public class Example : MonoBehaviour
{
    private void OnEnable()
    {
        NightPool.GameObjectInstantiated.AddListener(DoSomething);
    }

    private void OnDisable()
    {
        NightPool.GameObjectInstantiated.RemoveListener(DoSomething);
    }

    private void DoSomething(GameObject instantiatedGameObject)
    {
        Debug.Log(instantiatedGameObject);
    }
}
```
This event is also available in the `NightGameObjectPool`:
```csharp
public class Example : MonoBehaviour
{
    [SerializeField] private NightGameObjectPool _nightGameObjectPool;

    public void OnEnable()
    {
        _nightGameObjectPool.GameObjectInstantiated.AddListener(DoSomething);
    }

    private void OnDisable()
    {
        _nightGameObjectPool.GameObjectInstantiated.RemoveListener(DoSomething);
    }

    private void DoSomething(GameObject instantiatedGameObject)
    {
        Debug.Log(instantiatedGameObject);
    }
}
```
## Game Object Spawned
This event is available in `NightGameObjectPool` and called when a game object spawned.
```csharp
public class Example : MonoBehaviour
{
    [SerializeField] private NightGameObjectPool _nightGameObjectPool;

    public void OnEnable()
    {
        _nightGameObjectPool.GameObjectSpawned.AddListener(DoSomething);
    }

    private void OnDisable()
    {
        _nightGameObjectPool.GameObjectSpawned.RemoveListener(DoSomething);
    }

    private void DoSomething(GameObject spawnedGameObject)
    {
        Debug.Log(spawnedGameObject);
    }
}
```
## Game Object Despawned
This event is available in `NightGameObjectPool` and called when a game object despawned.
```csharp
public class Example : MonoBehaviour
{
    [SerializeField] private NightGameObjectPool _nightGameObjectPool;

    public void OnEnable()
    {
        _nightGameObjectPool.GameObjectDespawned.AddListener(DoSomething);
    }

    private void OnDisable()
    {
        _nightGameObjectPool.GameObjectDespawned.RemoveListener(DoSomething);
    }

    private void DoSomething(GameObject despawnedGameObject)
    {
        Debug.Log(despawnedGameObject);
    }
}
```
# 🔸 Enums
## Night Pool Mode
Should Night Pool focus more on performance or total safety?
| Mode | Info |
| ------ | ------ |
| Performance | If possible, Night Pool will skip some steps when spawning objects. **In this mode, the lossy scale of pools must be equal to Vector3.one!** |
| Safety | Night Pool will not skip any steps when spawning objects and you can set the pool scale to any value. |
## Behaviour On Capacity Reached
What to do when pool overflows?
| Behaviour | Info |
| ------ | ------ |
| Return Nullable Clone | Returns nullable clone. |
| Instantiate | Instantiates clone which will not be cached in a pool. **Such clones ignore all callbacks!** |
| Instantiate With Callbacks | Instantiates clone which will not be cached in a pool. |
| Recycle | New clones force older ones to despawn. |
| Throw Exception | Throws an exception. |
## Despawn Type
How clones must be despawned?
| Type | Info |
| ------ | ------ |
| Only Deactivate | Deactivates clones and put them back in a pool. |
| Deactivate And Set Null Parent | Does the same as the first one, but also changes the parent to null. |
| Deactivate And Hide | Does the same as the first one, but also changes the parent to a pool's game object. |
## Preload Type
Should the pools be preloaded?
| Type | Info |
| ------ | ------ |
| Disabled | Pools will not be preloaded. |
| On Awake | Pools will be preloaded on awake. |
| On Start | Pools will be preloaded on start. |
## Callbacks Type
Whether to make callbacks when clones spawn and despawn?
| Type | Info |
| ------ | ------ |
| None | Disables callbacks. |
| Interfaces | Finds the `ISpawnable`, `IDespawnable` or `IPoolable` interfaces using `GetComponents` and calls them. |
| Interfaces In Children | Finds the `ISpawnable`, `IDespawnable` or `IPoolable` interfaces using `GetComponentsInChildren` and calls them. |
| Send Message | Sends `OnSpawn` and `OnDespawn` messages by `GameObject.SendMessage`. |
| Broadcast Message | Broadcasts `OnSpawn` and `OnDespawn` messages by `GameObject.BroadcastMessage`. |
## Update Type
Determines in which update method the processing should be performed. Delayed despawns are currently handled in the selected update loop of this component. 
| Type | Info |
| ------ | ------ |
| Update | Processing will be done in an update loop. |
| Fixed Update | Processing will be done in a fixed update loop. |
| Late Update | Processing will be done in a late update loop. |
## Reaction On Repeated Delayed Despawn
What will be done if you try to destroy the same clone multiple times with a delay?
| Type | Info |
| ------ | ------ |
| Ignore | Ignores repeated delayed despawn of the clone. |
| Reset Delay | Resets the time of delayed despawn of the clone. |
| Reset Delay If New Time Is Less | Resets the time of delayed despawn of the clone if new time is less. |
| Reset Delay If New Time Is Greater | Resets the time of delayed despawn of the clone if new time is greater. |
| Throw Exception | Throws an exception. |
## Poolable Status
Shows the current status of the clone.
| Type | Info |
| ------ | ------ |
| Spawned | Clone is spawned. |
| Despawned | Clone is despawned. |
| SpawnedOverCapacity | Clone is spawned over capacity of the attached pool and will be just destroyed by `Despawn`. |

# 🔸 Extensions
## Particle System Auto Despawn
You can despawn a particle system when it finishes playing using extension method `DespawnOnComplete` from the `NightPoolExtensions` class:
```csharp
using NTC.Pool;

public class Example : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particleSystemPrefab;

    public void PlayParticle()
    {
        NightPool
            .Spawn(_particleSystemPrefab)
            .DespawnOnComplete();
    }
}
```
Additionally, this extension method returns `ParticleSystem`:
```csharp
ParticleSystem spawnedParticleSystem = NightPool.Spawn(_particleSystemPrefab).DespawnOnComplete();
```

# 🔸 Preload In Editor
You can also preload game objects in the editor.

To do this, make sure you set the required prefab in the `Prefab` field of the [`Night Game Object Pool`](#night-game-object-pool) and open the context menu of this component and execute the `Preload` method. As many game objects as you specified in the `Preload Size` field will be preloaded. Of course, if the pool has enough free space.

You can also preload only one game object. To do this, execute `Preload One` method instead of `Preload` in the same context menu.
> **Warning!** If you accidentally deleted one or more preloaded game objects and catch errors, then call the `Clear` method in the context menu of the required pool.

# 🔸 How to attach DI Container
If you want to inject instantiated game objects with the **Night Pool**, you can do it like this:
```csharp
public class DiContainerAttachmentExample : MonoBehaviour
{
    // Container of your DI solution.
    [Inject] private readonly Container _container;
    
    private void Awake()
    {
        NightPool.GameObjectInstantiated.AddListener(_container.InjectGameObject);
    }

    private void OnDestroy()
    {
        NightPool.GameObjectInstantiated.RemoveListener(_container.InjectGameObject);
    }
}
```
# 🔸 How to reset pooled Rigidbody
If you want to reset a rigidbody's velocity on despawn, you can do it like this:
```csharp
[RequireComponent(typeof(Rigidbody))]
public class Example : MonoBehaviour, IDespawnable
{
    void IDespawnable.OnDespawn()
    {
        Rigidbody rigidbodyToReset = GetComponent<Rigidbody>();
        Vector3 zeroVelocity = Vector3.zero;

        rigidbodyToReset.velocity = zeroVelocity;
        rigidbodyToReset.angularVelocity = zeroVelocity;
    }
}
```
