// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021-2023 Night Train Code
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using NTC.Pool.Attributes;
#endif

namespace NTC.Pool
{
    /// <summary>
    /// Gives you control over the spawning and despawning clones.
    /// </summary>
#if UNITY_EDITOR
    [DisallowMultipleComponent]
    [HelpURL(Constants.HelpUrl)]
    [AddComponentMenu(Constants.NightPoolComponentPath + "Night Game Object Pool")]
#endif
    public sealed class NightGameObjectPool : MonoBehaviour
    {
        [Header("Main")]
        [Tooltip("Prefab of this pool.")]
        [SerializeField] internal GameObject _prefab;
        [Tooltip(Constants.Tooltips.OverflowBehaviour)]
        [SerializeField] internal BehaviourOnCapacityReached _behaviourOnCapacityReached = Constants.DefaultBehaviourOnCapacityReached;
        [Tooltip(Constants.Tooltips.DespawnType)]
        [SerializeField] internal DespawnType _despawnType = Constants.DefaultDespawnType;
        [Tooltip("Capacity of this pool.")]
        [SerializeField, Delayed, Min(0)] private int _capacity = 32;
        
        [Header("Preload")]
        [Tooltip("Clones preload type of this pool.")]
        [SerializeField] private PreloadType _preloadType = PreloadType.Disabled;
        [Tooltip("Preload size of this pool.")]
        [SerializeField, Delayed, Min(0)] private int _preloadSize = 16;
        
        [Header("Callbacks")]
        [Tooltip(Constants.Tooltips.CallbacksType)]
        [SerializeField] internal CallbacksType _callbacksType = Constants.DefaultCallbacksType;
        
        [Header("Persistent")]
        [Tooltip("Should this pool be persistent?")]
        [SerializeField] internal bool _dontDestroyOnLoad;
        
        [Header("Debug")]
        [Tooltip("Should this pool find issues and log warnings?")]
        [SerializeField] internal bool _sendWarnings = true;
        
#if UNITY_EDITOR
        [Space, ReadOnlyInspectorField]
#endif
        [SerializeField] private int _allClonesCount;

#if UNITY_EDITOR
        [ReadOnlyInspectorField]
#endif
        [SerializeField] private int _spawnedClonesCount;
        
#if UNITY_EDITOR
        [ReadOnlyInspectorField]
#endif
        [SerializeField] private int _despawnedClonesCount;
        
#if UNITY_EDITOR
        [Space, ReadOnlyInspectorField]
#endif
        [SerializeField] private int _spawnsCount;
        
#if UNITY_EDITOR
        [ReadOnlyInspectorField]
#endif
        [SerializeField] private int _despawnsCount;
        
#if UNITY_EDITOR
        [ReadOnlyInspectorField]
#endif
        [SerializeField] private int _total;
        
#if UNITY_EDITOR
        [Space, ReadOnlyInspectorField]
#endif
        [SerializeField] private int _instantiated;

        [SerializeField, HideInInspector] private List<GameObject> _gameObjectsToPreload;
        [SerializeField, HideInInspector] private bool _hasPreloadedGameObjects;

        internal Transform _cachedTransform;
        internal Vector3 _regularPrefabScale;
        internal bool _isSetup;
        
        private readonly NightPoolList<Poolable> _spawnedPoolables 
            = new NightPoolList<Poolable>(Constants.DefaultPoolablesListCapacity);
        
        private readonly NightPoolList<Poolable> _despawnedPoolables 
            = new NightPoolList<Poolable>(Constants.DefaultPoolablesListCapacity);
        
        private NightPoolList<Poolable> _poolablesTemp;
        private Transform _prefabTransform;
#if UNITY_EDITOR
        private GameObject _cachedPrefab;
#endif

        /// <summary>
        /// The prefab attached to this pool.
        /// </summary>
        public GameObject AttachedPrefab => _prefab;
        
        /// <summary>
        /// Pool overflow behaviour.
        /// </summary>
        public BehaviourOnCapacityReached BehaviourOnCapacityReached => _behaviourOnCapacityReached;

        /// <summary>
        /// Clone despawn type.
        /// </summary>
        public DespawnType DespawnType => _despawnType;
        
        /// <summary>
        /// Callbacks on clone spawn and despawn.
        /// </summary>
        public CallbacksType CallbacksType => _callbacksType;
        
        /// <summary>
        /// Pool capacity.
        /// </summary>
        public int Capacity => _capacity;
        
        /// <summary>
        /// Number of spawned clones.
        /// </summary>
        public int SpawnedClonesCount => _spawnedClonesCount;
        
        /// <summary>
        /// Number of despawned clones.
        /// </summary>
        public int DespawnedClonesCount => _despawnedClonesCount;
        
        /// <summary>
        /// Number of all clones.
        /// </summary>
        public int AllClonesCount => _allClonesCount;
        
        /// <summary>
        /// Number of spawns.
        /// </summary>
        public int SpawnsCount => _spawnsCount;
        
        /// <summary>
        /// Number of despawns.
        /// </summary>
        public int DespawnsCount => _despawnsCount;

        /// <summary>
        /// Number of instantiates.
        /// </summary>
        public int InstantiatesCount => _instantiated;
        
        /// <summary>
        /// Total number of spawns and despawns.
        /// </summary>
        public int TotalActionsCount => _total;
        
        /// <summary>
        /// Has this pool registered as persistent?
        /// </summary>
        public bool HasRegisteredAsPersistent => NightPool.HasPoolRegisteredAsPersistent(this);

        /// <summary>
        /// The actions will be performed on a game object spawned by this pool.
        /// </summary>
        public readonly NightPoolEvent<GameObject> GameObjectSpawned = new NightPoolEvent<GameObject>();
        
        /// <summary>
        /// The actions will be performed on a game object despawned by this pool.
        /// </summary>
        public readonly NightPoolEvent<GameObject> GameObjectDespawned = new NightPoolEvent<GameObject>();
        
        /// <summary>
        /// The actions will be performed on a game object instantiated by this pool.
        /// </summary>
        public readonly NightPoolEvent<GameObject> GameObjectInstantiated = new NightPoolEvent<GameObject>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampCapacity();
            ClampPreloadSize();
            CheckPreloadedClonesForErrors();
            CheckForPrefabMatchOnPlay();
            CheckForPrefab(_prefab);
        }
#endif
        private void Awake()
        {
            if (_prefab == null)
                return;
            
            if (_dontDestroyOnLoad && HasRegisteredAsPersistent)
            {
                DestroyPool();
                return;
            }
            
            if (TrySetup(_prefab))
            {
                PreloadElements(PreloadType.OnAwake);
            }
        }

        private void Start()
        {
            if (_isSetup)
            {
                PreloadElements(PreloadType.OnStart);
                RaiseEventForPreloadedClonesAndClear();
            }
        }

        private void OnDestroy()
        {
            Clear();
            NightPool.UnregisterPool(this);
        }

        /// <summary>
        /// You can initialize the pool manually using this method.
        /// </summary>
        public void Init()
        {
            Init(_prefab);
        }

        /// <summary>
        /// You can initialize the pool manually using this method.
        /// </summary>
        /// <param name="prefab">Pool's prefab.</param>
        public void Init(GameObject prefab)
        {
#if DEBUG
            if (_isSetup)
            {
                if (_sendWarnings)
                {
                    Debug.LogWarning("The pool is already initialized!", this);
                }
                
                return;
            }
            
            if (prefab == null)
            {
                Debug.LogError("You are trying to initialize this pool with null prefab!", this);
                return;
            }

            if (_hasPreloadedGameObjects && _prefab != prefab)
            {
                Debug.LogError("This pool has already preloaded game objects " +
                               "and you are trying to initialize this pool with another prefab! " +
                               "Clear this pool or initialize one with correct prefab.", this);
                return;
            }
#endif
            if (TrySetup(prefab))
            {
                RaiseEventForPreloadedClonesAndClear();
            }
        }
        
        /// <summary>
        /// Populates this pool.
        /// </summary>
        /// <param name="count">Populate count.</param>
        /// <exception cref="Exception">Throws if pool is not setup.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Throws if populate count is smaller than zero.</exception>
        public void PopulatePool(int count)
        {
#if DEBUG
            if (_isSetup == false)
            {
                Debug.LogError($"The pool '{name}' is not setup!", this);
                return;
            }

            if (Application.isPlaying == false)
            {
                Debug.LogError($"You are trying to populate the pool '{name}' when the application is not playing!", 
                    this); 
                return;
            }

            if (count < 0)
            {
                Debug.LogError("The count of populating must not be less than zero!", this);
                return;
            }
#endif
            for (var i = 0; i < count; i++)
            {
                if (_allClonesCount >= _capacity)
                {
#if DEBUG
                    if (_sendWarnings)
                    {
                        Debug.LogWarning($"The pool {name} reached max capacity!");
                    }
#endif
                    return;
                }
                
                AddPoolableToList(_despawnedPoolables, InstantiateAndSetupPoolable(true), 
                    ref _despawnedClonesCount);
            }
        }
        
        /// <summary>
        /// Sets the capacity of this pool.
        /// </summary>
        /// <param name="capacity">New pool capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws if capacity is smaller than zero or smaller than all clones count.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCapacity(int capacity)
        {
#if DEBUG
            if (capacity < 0)
            {
                Debug.LogError($"Capacity of the pool '{name}' can't be less than zero!", this);
                return;
            }

            if (capacity < _allClonesCount)
            {
                Debug.LogError($"Capacity of the pool '{name}' must not be less than the number of all clones!", this);
                return;
            }

            if (_hasPreloadedGameObjects && _capacity < _gameObjectsToPreload.Count)
            {
                Debug.LogError(
                    $"Capacity of the pool '{name}' must not be less than the number of preloaded clones!", this);
                return;
            }

            if (_sendWarnings && capacity == 0)
            {
                Debug.LogWarning($"Capacity of the pool '{name}' is equals zero.");
            }
#endif
            _capacity = capacity;
            _preloadSize = Mathf.Clamp(_preloadSize, 0, _capacity);
        }

        /// <summary>
        /// Sets the behaviour on capacity reached of this pool.
        /// </summary>
        /// <param name="behaviourOnCapacityReached">New behaviour.</param>
        public void SetBehaviourOnCapacityReached(BehaviourOnCapacityReached behaviourOnCapacityReached)
        {
            _behaviourOnCapacityReached = behaviourOnCapacityReached;
        }

        /// <summary>
        /// Sets the despawn type of this pool.
        /// </summary>
        /// <param name="despawnType">New despawn type.</param>
        public void SetDespawnType(DespawnType despawnType)
        {
            _despawnType = despawnType;
        }
        
        /// <summary>
        /// Sets the callbacks type of this pool.
        /// </summary>
        /// <param name="callbacksType">New callbacks type.</param>
        public void SetCallbacksType(CallbacksType callbacksType)
        {
            _callbacksType = callbacksType;
        }

        /// <summary>
        /// Sets the warnings active of this pool.
        /// </summary>
        /// <param name="active">New warnings active status.</param>
        public void SetWarningsActive(bool active)
        {
            _sendWarnings = active;
        }

        /// <summary>
        /// Performs an action for each clone.
        /// </summary>
        /// <param name="action">Action to perform.</param>
        public void ForEachClone(Action<GameObject> action)
        {
            ForEach(_spawnedPoolables, action);
            ForEach(_despawnedPoolables, action);
        }
        
        /// <summary>
        /// Performs an action for each spawned clone.
        /// </summary>
        /// <param name="action">Action to perform.</param>
        public void ForEachSpawnedClone(Action<GameObject> action)
        {
            ForEach(_spawnedPoolables, action);
        }

        /// <summary>
        /// Performs an action for each despawned clone.
        /// </summary>
        /// <param name="action">Action to perform.</param>
        public void ForEachDespawnedClone(Action<GameObject> action)
        {
            ForEach(_despawnedPoolables, action);
        }

        /// <summary>
        /// Destroys this pool with clones.
        /// </summary>
        public void DestroyPool()
        {
            Clear();
            Destroy(gameObject);
        }

        /// <summary>
        /// Destroys this pool with clones immediate.
        /// </summary>
        public void DestroyPoolImmediate()
        {
            Clear();
            DestroyImmediate(gameObject);
        }
        
        /// <summary>
        /// Destroys all clones in this pool (also destroys preloaded clones).
        /// </summary>
#if UNITY_EDITOR
        [ContextMenu("Clear")]
#endif
        public void Clear()
        {
            ClearEvents();
            ClearGameObjectsToPreload();
            DestroyAllClonesImmediate();
            ResetCounts();
        }
        
        /// <summary>
        /// Destroys all clones in this pool.
        /// </summary>
        public void DestroyAllClones()
        {
            DestroySpawnedClones();
            DestroyDespawnedClones();
        }

        /// <summary>
        /// Destroys spawned clones in this pool.
        /// </summary>
        public void DestroySpawnedClones()
        {
            DisposePoolablesInList(_spawnedPoolables, ref _spawnedClonesCount, false);
        }

        /// <summary>
        /// Destroys despawned clones in this pool.
        /// </summary>
        public void DestroyDespawnedClones()
        {
            DisposePoolablesInList(_despawnedPoolables, ref _despawnedClonesCount, false);
        }

        /// <summary>
        /// Destroys all clones in this pool immediately.
        /// </summary>
        public void DestroyAllClonesImmediate()
        {
            DestroySpawnedClonesImmediate();
            DestroyDespawnedClonesImmediate();
        }

        /// <summary>
        /// Destroys spawned clones in this pool immediately.
        /// </summary>
        public void DestroySpawnedClonesImmediate()
        {
            DisposePoolablesInList(_spawnedPoolables, ref _spawnedClonesCount, true);
        }

        /// <summary>
        /// Destroys despawned clones in this pool immediately.
        /// </summary>
        public void DestroyDespawnedClonesImmediate()
        {
            DisposePoolablesInList(_despawnedPoolables, ref _despawnedClonesCount, true);
        }

        /// <summary>
        /// Despawns all spawned clones.
        /// </summary>
        public void DespawnAllClones()
        {
            _poolablesTemp ??= new NightPoolList<Poolable>(Constants.DefaultPoolablesListCapacity);

            for (int i = 0; i < _spawnedPoolables._count; i++)
            {
                _poolablesTemp.Add(_spawnedPoolables._components[i]);
            }

            for (int i = 0; i < _poolablesTemp._count; i++)
            {
                NightPool.DespawnImmediate(_poolablesTemp._components[i]);
            }

            if (_poolablesTemp._count > 0)
            {
                _poolablesTemp.Clear();
                _poolablesTemp.SetCapacity(Constants.DefaultPoolablesListCapacity);
            }
        }

        internal bool TrySetup(GameObject prefab)
        {
            if (_isSetup)
                return false;
            
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                Debug.LogError("You can't setup a pool when application is not playing!", this);
                return false;
            }
            
            if (NightPool.s_checkForPrefab)
            {
                if (CheckForPrefab(prefab) == false)
                {
                    return false;
                }
            }

            _cachedPrefab = prefab;
#endif
            _prefab = prefab;
            _cachedTransform = transform;
            _prefabTransform = prefab.transform;
            _regularPrefabScale = _prefabTransform.localScale;
            
            if (_dontDestroyOnLoad)
            {
                if (TryRegisterPoolAsPersistent() == false)
                {
                    return false;
                }
            }

            if (_hasPreloadedGameObjects)
            {
                SetupPreloadedClones();
            }
            
            NightPool.RegisterPool(this);
            
            _isSetup = true;
            return true;
        }

        internal void UnregisterPoolable(Poolable poolable)
        {
            RemovePoolableUnorderedFromList(_spawnedPoolables, poolable, ref _spawnedClonesCount);
            RemovePoolableUnorderedFromList(_despawnedPoolables, poolable, ref _despawnedClonesCount);
        }

        internal void Get(out GettingPoolableArguments arguments)
        {
            if (_despawnedPoolables._count <= 0)
            {
                if (_allClonesCount >= _capacity)
                {
                    if (_behaviourOnCapacityReached == BehaviourOnCapacityReached.Recycle)
                    {
                        Poolable poolable = _spawnedPoolables._components[0];
                        _spawnedPoolables.RemoveAt(0);
                        _spawnedPoolables.Add(poolable);
                        arguments = new GettingPoolableArguments(poolable, false);
                        return;
                    }
                    
                    if (_behaviourOnCapacityReached == BehaviourOnCapacityReached.InstantiateWithCallbacks)
                    {
                        InstantiatePoolableOverCapacity(out arguments);
                        return;
                    }
                    
                    if (_behaviourOnCapacityReached == BehaviourOnCapacityReached.Instantiate)
                    {
                        InstantiatePoolableOverCapacity(out arguments);
                        return;
                    }

                    if (_behaviourOnCapacityReached == BehaviourOnCapacityReached.ReturnNullableClone)
                    {
                        arguments = new GettingPoolableArguments(null, true);
                        return;
                    }

                    if (_behaviourOnCapacityReached == BehaviourOnCapacityReached.ThrowException)
                    {
#if DEBUG
                        Debug.LogException(new Exception("Capacity reached! You can't spawn a new clone!"), this);
#endif
                        arguments = new GettingPoolableArguments(null, true);
                        return;
                    }
                }

                arguments = new GettingPoolableArguments(InstantiateAndSetupPoolable(false), false);
                AddPoolableToList(_spawnedPoolables, arguments.Poolable, ref _spawnedClonesCount);
                return;
            }

            arguments = new GettingPoolableArguments(_despawnedPoolables._components[0], false);
            AddPoolableToList(_spawnedPoolables, _despawnedPoolables._components[0], ref _spawnedClonesCount);
            RemoveFirstPoolableUnordered(_despawnedPoolables, ref _despawnedClonesCount);
        }

        internal void Release(Poolable poolable)
        {
            if (poolable._status == PoolableStatus.Despawned)
            {
#if DEBUG
                if (_sendWarnings)
                {
                    Debug.LogWarning($"The poolable '{poolable._gameObject}' has already despawned!", 
                        poolable._gameObject);
                }
#endif
                return;
            }
            
            poolable._gameObject.SetActive(false);

            switch (_despawnType)
            {
                case DespawnType.DeactivateAndHide: HidePoolable(poolable); break;
                case DespawnType.DeactivateAndSetNullParent: SetPoolableParentAsNull(poolable); break;
                case DespawnType.OnlyDeactivate: break;
                default: throw new ArgumentOutOfRangeException(nameof(_despawnType));
            }

            AddPoolableToList(_despawnedPoolables, poolable, ref _despawnedClonesCount);
            RemovePoolableUnorderedFromList(_spawnedPoolables, poolable, ref _spawnedClonesCount);
        }

        internal void RaiseGameObjectSpawnedCallback(GameObject spawnedGameObject)
        {
            RaisePoolActionCallback(spawnedGameObject, ref _spawnsCount, GameObjectSpawned);
        }

        internal void RaiseGameObjectDespawnedCallback(GameObject despawnedGameObject)
        {
            RaisePoolActionCallback(despawnedGameObject, ref _despawnsCount, GameObjectDespawned);
        }

        private void InstantiatePoolableOverCapacity(out GettingPoolableArguments arguments)
        {
            GameObject newGameObject = Instantiate(_prefab);
            SetupPoolableAsSpawnedOverCapacity(newGameObject, out Poolable poolable);
            arguments = new GettingPoolableArguments(poolable, false);
            RaiseGameObjectInstantiatedCallback(poolable._gameObject);
        }

        private void RaisePoolActionCallback(GameObject clone, ref int actionCount, 
            NightPoolEvent<GameObject> poolEvent)
        {
            _total++;
            actionCount++;
            poolEvent.RaiseEvent(clone);
        }

        private void RaiseEventForPreloadedClonesAndClear()
        {
            if (_hasPreloadedGameObjects)
            {
                for (int i = 0; i < _gameObjectsToPreload.Count; i++)
                {
                    GameObjectInstantiated.RaiseEvent(_gameObjectsToPreload[i]);
                }

                _hasPreloadedGameObjects = false;
                _gameObjectsToPreload = null;
            }
        }
        
        private void HidePoolable(Poolable poolable)
        {
            poolable._transform.SetParent(_cachedTransform, true);
        }

        private void SetPoolableParentAsNull(Poolable poolable)
        {
            poolable._transform.SetParent(null, false);
        }
        
        private void RaiseGameObjectInstantiatedCallback(GameObject instantiatedGameObject)
        {
            _instantiated++;
            GameObjectInstantiated.RaiseEvent(instantiatedGameObject);
        }

#if UNITY_EDITOR
        private bool CheckForPrefab(GameObject gameObjectToCheck)
        {
            if (gameObjectToCheck == null)
                return false;

            if (gameObjectToCheck.scene.isLoaded)
            {
                Debug.LogError("You can't set a game object from the scene as a prefab!", this);
                _prefab = null;
                return false;
            }

            if (PrefabUtility.IsPartOfAnyPrefab(gameObjectToCheck) == false)
            {
                Debug.LogError($"The '{gameObjectToCheck}' is not a prefab!", this);
                _prefab = null;
                return false;
            }

            return true;
        }

        private void ClampCapacity()
        {
            if (_hasPreloadedGameObjects && _capacity < _gameObjectsToPreload.Count)
            {
                Debug.LogError("Capacity must not be less than the number of preloaded clones!", this);
                _capacity = _gameObjectsToPreload.Count;
            }
            
            if (_despawnedPoolables != null)
            {
                if (_capacity < _allClonesCount)
                {
                    Debug.LogError("Capacity must not be less than the number of all clones!", this);
                    _capacity = _allClonesCount;
                }
            }
        }
        
        private void ClampPreloadSize()
        {
            if (_preloadSize > _capacity)
            {
                _preloadSize = _capacity;
            }
        }
        
        private void CheckPreloadedClonesForErrors()
        {
            if (_hasPreloadedGameObjects)
            {
                bool isApplicationPlaying = Application.isPlaying;
                
                if (_prefab == null)
                {
                    Debug.LogError("You have preloaded game objects in this pool, but prefab is null now! " +
                                   "Set the correct prefab to fix this or clear this pool.", this);
                }

                for (int i = 0; i < _gameObjectsToPreload.Count; i++)
                {
                    GameObject clone = _gameObjectsToPreload[i];
                    
                    if (clone == null)
                    {
                        Debug.LogError("One of the preloaded game objects of this pool is null! " +
                                       "Clear this pool to fix this.", this);
                        return;
                    }
                    
                    if (isApplicationPlaying == false && 
                        PrefabUtility.GetCorrespondingObjectFromSource(clone) != _prefab)
                    {
                        Debug.LogError("Your preloaded game objects no longer match the prefab. " +
                                       "Clear this pool or set the correct prefab.", this);
                        return;
                    }
                }
            }
        }

        private void CheckForPrefabMatchOnPlay()
        {
            if (_isSetup && Application.isPlaying)
            {
                if (_cachedPrefab != null && _prefab != _cachedPrefab)
                {
                    _prefab = _cachedPrefab;
                }
            }
        }
#endif
        
        private static void ForEach(NightPoolList<Poolable> list, Action<GameObject> action)
        {
#if DEBUG
            if (action == null)
                throw new ArgumentNullException(nameof(action));
#endif
            for (int i = 0; i < list._count; i++)
            {
                action.Invoke(list._components[i]._gameObject);
            }
        }
        
        private void DisposePoolablesInList(NightPoolList<Poolable> nightPoolList, ref int count, bool immediately)
        {
            for (int i = 0; i < nightPoolList._count; i++)
            {
                nightPoolList._components[i].Dispose(immediately);
                _allClonesCount--;
            }
            
            nightPoolList.Clear();
            count = 0;
        }
        
        private bool TryRegisterPoolAsPersistent()
        {
            if (NightPool.HasPoolRegisteredAsPersistent(this) == false)
            {
#if DEBUG
                if (_cachedTransform.parent != null)
                {
                    Debug.LogError("The pool can't be persistent! " +
                                   "Because this GameObject has parent Transform and " +
                                   "DontDestroyOnLoad only works for root GameObjects or components " +
                                   "on root GameObjects.", this);
                    return false;
                }
#endif
                _dontDestroyOnLoad = true;
                DontDestroyOnLoad(gameObject);
                NightPool.RegisterPersistentPool(this);
                return true;
            }
            
            DestroyPool();
            return false;
        }
        
        private void AddPoolableToList(NightPoolList<Poolable> nightPoolList,
            Poolable poolable, ref int count)
        {
            nightPoolList.Add(poolable);
            count++;
            _allClonesCount++;
        }
        
        private void RemovePoolableUnorderedFromList(NightPoolList<Poolable> nightPoolList,
            Poolable poolable, ref int count)
        {
            for (int i = 0; i < nightPoolList._count; i++)
            {
                if (nightPoolList._components[i] == poolable)
                {
                    nightPoolList.RemoveUnorderedAt(i);
                    count--;
                    _allClonesCount--;
                    return;
                }
            }
        }

        private void RemoveFirstPoolableUnordered(NightPoolList<Poolable> nightPoolList, ref int count)
        {
            nightPoolList.RemoveUnorderedAt(0);
            count--;
            _allClonesCount--;
        }

#if UNITY_EDITOR
        [ContextMenu("Preload")]
        private void Preload()
        {
            for (int i = 0; i < _preloadSize; i++)
            {
                if (TryPreloadGameObject() == false)
                {
                    return;
                }
            }
        }

        [ContextMenu("Preload One")]
        private void PreloadOne()
        {
            TryPreloadGameObject();
        }

        private bool TryPreloadGameObject()
        {
            if (CanPreloadGameObject())
            {
                PreloadGameObject();
                return true;
            }

            return false;
        }

        private bool CanPreloadGameObject()
        {
            if (_prefab == null)
            {
                Debug.LogError($"The prefab of the pool '{name}' is null!", this);
                return false;
            }
            
            if (CheckForPrefab(_prefab) == false)
            {
                return false;
            }
            
            if (_gameObjectsToPreload.Count >= _capacity || _allClonesCount >= _capacity)
            {
                if (_sendWarnings)
                {
                    Debug.LogWarning("Capacity reached! You can't preload more game objects!", this);
                }
                
                return false;
            }

            return true;
        }
        
        private void PreloadGameObject()
        {
            GameObject gameObjectToPreload = PrefabUtility.InstantiatePrefab(_prefab, transform) as GameObject;

            if (gameObjectToPreload == null) 
                return;
            
            gameObjectToPreload.SetActive(false);

            _instantiated++;

            if (Application.isPlaying)
            {
                SetupPoolableAsDefault(gameObjectToPreload, out Poolable poolable);
                AddPoolableToList(_despawnedPoolables, poolable, ref _despawnedClonesCount);
            }
            else
            {
                _hasPreloadedGameObjects = true;
                _gameObjectsToPreload.Add(gameObjectToPreload);
            }
        }
#endif
        
        private void PreloadElements(PreloadType requiredType)
        {
            if (_preloadType != requiredType)
                return;

            if (_allClonesCount >= _capacity)
                return;
            
            PopulatePool(_preloadSize);
        }

        private void SetupPreloadedClones()
        {
            for (var i = 0; i < _gameObjectsToPreload.Count; i++)
            {
                GameObject clone = _gameObjectsToPreload[i];
#if DEBUG
                if (clone == null)
                {
                    Debug.LogError($"One of the preloaded game objects has been destroyed! Clear the '{name}' pool " +
                                   "from the component context menu when the application is not playing " +
                                   "to fix this!", this);
                    continue;
                }
#endif
                SetupPoolableAsDefault(clone, out Poolable poolable);
                AddPoolableToList(_despawnedPoolables, poolable, ref _despawnedClonesCount);
            }
        }
        
        private Poolable InstantiateAndSetupPoolable(bool isPopulatingPool)
        {
            GameObject newGameObject = Instantiate(_prefab);
            SetupPoolableAsDefault(newGameObject, out Poolable poolable);
            
            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(newGameObject);
            }
            
            if (isPopulatingPool)
            {
                poolable._gameObject.SetActive(false);
                poolable._transform.SetParent(_isSetup ? _cachedTransform : transform, false);
            }

            NightPool.GameObjectInstantiated.RaiseEvent(newGameObject);
            RaiseGameObjectInstantiatedCallback(newGameObject);
            return poolable;
        }

        private void SetupPoolableAsDefault(GameObject clone, out Poolable poolable)
        {
            poolable = CreatePoolable(clone);
            poolable.SetupAsDefault();
        }

        private void SetupPoolableAsSpawnedOverCapacity(GameObject clone, out Poolable poolable)
        {
            poolable = CreatePoolable(clone);
            poolable.SetupAsSpawnedOverCapacity();
        }

        private Poolable CreatePoolable(GameObject clone)
        {
            return new Poolable
            {
                _pool = this,
                _gameObject = clone,
                _transform = clone.transform   
            };
        }

        private void ClearGameObjectsToPreload()
        {
            if (_gameObjectsToPreload != null)
            {
                for (int i = 0; i < _gameObjectsToPreload.Count; i++)
                {
                    DestroyImmediate(_gameObjectsToPreload[i]);
                }
                
                _gameObjectsToPreload.Clear();
                _hasPreloadedGameObjects = false;
            }
        }

        private void ResetCounts()
        {
            _allClonesCount = 0;
            _instantiated = 0;
            _spawnsCount = 0;
            _despawnsCount = 0;
            _total = 0;
        }

        private void ClearEvents()
        {
            GameObjectSpawned.Clear();
            GameObjectDespawned.Clear();
            GameObjectInstantiated.Clear();
        }
    }
}

#if UNITY_EDITOR
namespace NTC.Pool.Attributes
{
    [CustomPropertyDrawer(typeof(ReadOnlyInspectorFieldAttribute))]
    public sealed class ReadOnlyInspectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ReadOnlyInspectorFieldAttribute : PropertyAttribute { }
}
#endif