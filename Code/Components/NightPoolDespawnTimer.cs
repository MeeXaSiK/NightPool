using UnityEngine;

namespace NTC.Pool
{
#if UNITY_EDITOR
    [HelpURL(Constants.HelpUrl)]
    [AddComponentMenu(Constants.NightPoolComponentPath + "Night Pool Despawn Timer")]
#endif
    public sealed class NightPoolDespawnTimer : MonoBehaviour, ISpawnable
    {
        [SerializeField] private UpdateType _updateType = UpdateType.Update;
        [SerializeField, Min(0f)] private float _timeToDespawn = 3f;

        private bool _hasDespawnPerformed;
        private float _elapsedTime;

#if DEBUG
        private void Start()
        {
            if (NightPool.IsClone(gameObject) == false)
            {
                Debug.LogError("You have a Despawn Timer added to a game object that is not a clone!", this);
            }
            
            if (NightPool.TryGetPoolByClone(gameObject, out NightGameObjectPool pool))
            {
                if (pool.BehaviourOnCapacityReached == BehaviourOnCapacityReached.Recycle)
                {
                    if (pool.CallbacksType == CallbacksType.None)
                    {
                        Debug.LogWarning("Callbacks are disabled in this recycling pool! " +
                                         "In a recycling pool, the timer may not reset when spawning! " +
                                         "Enable callbacks in the pool or choose another " +
                                         "'Behaviour On Capacity Reached' option.", pool);
                    }
                }
            }
        }
#endif
        /// <summary>
        /// Resets the timer.
        /// </summary>
        public void OnSpawn()
        {
            ResetTimer();
        }

        private void OnDisable()
        {
            ResetTimer();
        }

        private void Update()
        {
            if (_updateType == UpdateType.Update)
            {
                HandleDespawn(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (_updateType == UpdateType.FixedUpdate)
            {
                HandleDespawn(Time.fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            if (_updateType == UpdateType.LateUpdate)
            {
                HandleDespawn(Time.deltaTime);
            }
        }

        private void HandleDespawn(float deltaTime)
        {
            if (IsDespawnMoment(deltaTime))
            {
                NightPool.Despawn(gameObject);
            }
        }

        private bool IsDespawnMoment(float deltaTime)
        {
            if (_hasDespawnPerformed)
                return false;
            
            _elapsedTime += deltaTime;

            if (_elapsedTime >= _timeToDespawn)
                return true;

            return false;
        }

        private void ResetTimer()
        {
            _hasDespawnPerformed = false;
            _elapsedTime = 0f;
        }
    }
}