// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021-2023 Night Train Code
// ----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace NTC.Pool
{
#if UNITY_EDITOR
    [DisallowMultipleComponent]
    [HelpURL(Constants.HelpUrl)]
    [AddComponentMenu(Constants.NightPoolComponentPath + "Night Pool Global")]
#endif
    public sealed class NightPoolGlobal : MonoBehaviour
    {
        [Header("Main")] 
        [Tooltip(Constants.Tooltips.GlobalUpdateType)]
        [SerializeField] private UpdateType _updateType = UpdateType.Update;
        
        [Header("Preload Pools")]
        [Tooltip(Constants.Tooltips.GlobalPreloadType)]
        [SerializeField] private PreloadType preloadPoolsType = PreloadType.Disabled;
        
        [Tooltip(Constants.Tooltips.PoolsToPreload)]
        [SerializeField] private PoolsPreset poolsPreset;

        [Header("Global Pool Settings")] 
        [Tooltip(Constants.Tooltips.OverflowBehaviour)]
        [SerializeField] internal BehaviourOnCapacityReached _behaviourOnCapacityReached = Constants.DefaultBehaviourOnCapacityReached;
        
        [Tooltip(Constants.Tooltips.DespawnType)]
        [SerializeField] internal DespawnType _despawnType = Constants.DefaultDespawnType;
        
        [Tooltip(Constants.Tooltips.CallbacksType)]
        [SerializeField] internal CallbacksType _callbacksType = Constants.DefaultCallbacksType;
        
        [Tooltip(Constants.Tooltips.Capacity)]
        [SerializeField, Min(0)] internal int _capacity = 64;
        
        [Tooltip(Constants.Tooltips.Persistent)]
        [SerializeField] internal bool _dontDestroyOnLoad;

        [Tooltip(Constants.Tooltips.Warnings)]
        [SerializeField] internal bool _sendWarnings = true;
        
        [Header("Safety")] 
        [Tooltip(Constants.Tooltips.NightPoolMode)]
        [SerializeField] internal NightPoolMode _nightPoolMode = Constants.DefaultNightPoolMode;
        
        [Tooltip(Constants.Tooltips.DelayedDespawnReaction)]
        [SerializeField] internal ReactionOnRepeatedDelayedDespawn _reactionOnRepeatedDelayedDespawn = Constants.DefaultDelayedDespawnHandleType;
        
        [Tooltip(Constants.Tooltips.DespawnPersistentClonesOnDestroy)]
        [SerializeField] private bool _despawnPersistentClonesOnDestroy = true;
        
        [Tooltip(Constants.Tooltips.CheckClonesForNull)]
        [SerializeField] private bool _checkClonesForNull = true;
        
        [Tooltip(Constants.Tooltips.CheckForPrefab)]
        [SerializeField] private bool _checkForPrefab = true;
        
        [Tooltip(Constants.Tooltips.ClearEventsOnDestroy)]
        [SerializeField] private bool _clearEventsOnDestroy;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                NightPool.s_nightPoolMode = _nightPoolMode;
                NightPool.s_checkForPrefab = _checkForPrefab;
                NightPool.s_checkClonesForNull = _checkClonesForNull;
                NightPool.s_despawnPersistentClonesOnDestroy = _despawnPersistentClonesOnDestroy;
            }
        }
#endif
        private void Awake()
        {
            Initialize();
            PreloadPools(PreloadType.OnAwake);
        }

        private void Start()
        {
            PreloadPools(PreloadType.OnStart);
        }

        private void Update()
        {
            if (_updateType == UpdateType.Update)
            {
                HandleDespawnRequests(Time.deltaTime);
            }
        }
        
        private void FixedUpdate()
        {
            if (_updateType == UpdateType.FixedUpdate)
            {
                HandleDespawnRequests(Time.fixedDeltaTime);
            }
        }
        
        private void LateUpdate()
        {
            if (_updateType == UpdateType.LateUpdate)
            {
                HandleDespawnRequests(Time.deltaTime);
            }
        }

        private void OnApplicationQuit()
        {
            NightPool.s_isApplicationQuitting = true;
        }
        
        private void OnDestroy()
        {
            NightPool.ResetPool();

            if (_clearEventsOnDestroy || NightPool.s_isApplicationQuitting)
            {
                NightPool.GameObjectInstantiated.Clear();
            }
        }

        private void Initialize()
        {
#if DEBUG
            if (NightPool.s_instance != null && NightPool.s_instance != this)
                throw new Exception($"The number of {nameof(NightPool)} instances in the scene is greater than one!"); 
            
            if (enabled == false)
                Debug.LogWarning($"The <{nameof(NightPoolGlobal)}> instance is disabled! " +
                                 "Some functions may not work because of this!", this);
#endif
            NightPool.s_isApplicationQuitting = false;
            NightPool.s_instance = this;
            NightPool.s_hasTheNightPoolInitialized = true;
            NightPool.s_nightPoolMode = _nightPoolMode;
            NightPool.s_checkForPrefab = _checkForPrefab;
            NightPool.s_checkClonesForNull = _checkClonesForNull;
            NightPool.s_despawnPersistentClonesOnDestroy = _despawnPersistentClonesOnDestroy;
        }

        private void PreloadPools(PreloadType requiredType)
        {
            if (requiredType != preloadPoolsType)
                return;
            
            NightPool.InstallPools(poolsPreset);
        }

        private void HandleDespawnRequests(float deltaTime)
        {
            for (int i = 0; i < NightPool.DespawnRequests._count; i++)
            {
                ref DespawnRequest request = ref NightPool.DespawnRequests._components[i];

                if (request.Poolable._status == PoolableStatus.Despawned)
                {
                    NightPool.DespawnRequests.RemoveUnorderedAt(i);
                    continue;
                }
                
                request.TimeToDespawn -= deltaTime;
                
                if (request.TimeToDespawn <= 0f)
                {
                    NightPool.DespawnImmediate(request.Poolable);
                    NightPool.DespawnRequests.RemoveUnorderedAt(i);
                }
            }
        }
    }
}