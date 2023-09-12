using UnityEngine;

namespace NTC.Pool
{
    public static class NightPoolExtensions
    {
        /// <summary>
        /// Despawns a particle system when it finishes playing.
        /// </summary>
        /// <param name="particleSystem">A particle system to despawn on complete.</param>
        /// <returns>A particle system to despawn on complete.</returns>
        public static ParticleSystem DespawnOnComplete(this ParticleSystem particleSystem)
        {
            NightPool.Despawn(particleSystem.gameObject, particleSystem.main.duration);
            return particleSystem;
        }
    }
}