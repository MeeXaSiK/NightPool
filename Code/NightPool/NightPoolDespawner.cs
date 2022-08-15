// ----------------------------------------------------------------------------
// The MIT License
// NightPool is an object pool for Unity https://github.com/MeeXaSiK/NightPool
// Copyright (c) 2021-2022 Night Train Code
// ----------------------------------------------------------------------------

using UnityEngine;

namespace NTC.Global.Pool
{
    public class NightPoolDespawner : MonoBehaviour
    {
        [SerializeField] private float timeToDespawn = 3f;

        private bool processed;
        private float timer;

        private void OnEnable()
        {
            Restore();
        }

        private void OnDisable()
        {
            Restore();
        }

        private void Update()
        {
            if (IsDespawnMoment() == false)
                return;
            
            NightPool.Despawn(gameObject);
            
            OnProcessed();
        }

        private bool IsDespawnMoment()
        {
            if (processed)
                return false;
            
            timer += Time.deltaTime;

            if (timer >= timeToDespawn)
                return true;

            return false;
        }

        private void Restore()
        {
            timer = 0f;
            processed = false;
        }

        private void OnProcessed()
        {
            processed = true;
            timer = 0f;
        }
    }
}