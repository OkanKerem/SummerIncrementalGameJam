using System;
using System.Collections.Generic;
using UnityEngine;

namespace Universes.Stars
{
    public class Star
    {
        public int Id { get; }
        public string Name { get; }
        public Vector2 Position { get; }
        public float Age { get; set; }
        public StarStage Stage => StarStageUtility.FromAge(Age);
        public bool IsDead => Stage == StarStage.Dead;
        public List<int> PlanetIds { get; } = new();

        private StarStage _lastStage = StarStage.Yellow;

        public event Action<Star> OnStageChanged;
        public event Action<Star> OnSupernova;
        public event Action<Star> OnDied;

        public Star(int id, Vector2 position)
        {
            Id = id;
            Name = Universes.Core.CosmicNameGenerator.GenerateStarName(id);
            Position = position;
        }

        public void TickAge(float deltaTime, double ageRate)
        {
            ApplyAgeDelta((float)(ageRate * deltaTime));
        }

        public void ApplyClickAgePenalty(float penalty)
        {
            ApplyAgeDelta(penalty);
        }

        private void ApplyAgeDelta(float delta)
        {
            if (IsDead)
                return;

            var previousStage = Stage;
            Age = Mathf.Min(Age + delta, 100f);
            var currentStage = Stage;

            if (currentStage != previousStage)
            {
                OnStageChanged?.Invoke(this);
                if (currentStage == StarStage.Supernova)
                    OnSupernova?.Invoke(this);
            }

            if (Age >= 100f && _lastStage != StarStage.Dead)
            {
                _lastStage = StarStage.Dead;
                OnDied?.Invoke(this);
            }
            else
            {
                _lastStage = currentStage;
            }
        }

        public bool CanHostPlanet() => StarStageUtility.AllowsPlanets(Stage) && !IsDead;
    }
}
