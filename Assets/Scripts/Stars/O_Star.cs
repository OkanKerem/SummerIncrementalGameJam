using System;
using System.Collections.Generic;
using UnityEngine;

namespace Universes.Stars
{
    public class O_Star
    {
        public int Id { get; }
        public string Name { get; }
        public Vector2 Position { get; }
        public float Age { get; set; }
        public O_StarStage Stage => O_StarStageUtility.FromAge(Age);
        public bool IsDead => Stage == O_StarStage.Dead;
        public List<int> PlanetIds { get; } = new();

        private O_StarStage _lastStage = O_StarStage.Yellow;

        public event Action<O_Star> OnStageChanged;
        public event Action<O_Star> OnSupernova;
        public event Action<O_Star> OnDied;

        public O_Star(int id, Vector2 position)
        {
            Id = id;
            Name = Universes.Core.O_CosmicNameGenerator.GenerateStarName(id);
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
                if (currentStage == O_StarStage.Supernova)
                    OnSupernova?.Invoke(this);
            }

            if (Age >= 100f && _lastStage != O_StarStage.Dead)
            {
                _lastStage = O_StarStage.Dead;
                OnDied?.Invoke(this);
            }
            else
            {
                _lastStage = currentStage;
            }
        }

        public bool CanHostPlanet() => O_StarStageUtility.AllowsPlanets(Stage) && !IsDead;
    }
}
