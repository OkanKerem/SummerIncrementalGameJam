using System;
using UnityEngine;

namespace Universes.Core
{
    public class GameClock : MonoBehaviour
    {
        public const float TickInterval = 0.1f;

        public event Action<float> OnTick;

        private float _accumulator;

        private void Update()
        {
            _accumulator += Time.deltaTime;
            while (_accumulator >= TickInterval)
            {
                _accumulator -= TickInterval;
                OnTick?.Invoke(TickInterval);
            }
        }
    }
}
