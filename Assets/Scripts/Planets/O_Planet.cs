using System;
using System.Collections.Generic;
using UnityEngine;

namespace Universes.Planets
{
    public class O_Planet
    {
        public int Id { get; }
        public string Name { get; }
        public int HostStarId { get; }
        public bool HasLife { get; set; }
        public bool IsUnstable { get; set; }
        public float OrbitAngle { get; }
        public float OrbitRadius { get; }
        public int OrbitIndex { get; }

        public O_Planet(int id, int hostStarId, int orbitIndex, float orbitAngle, float orbitRadius)
        {
            Id = id;
            Name = Universes.Core.O_CosmicNameGenerator.GeneratePlanetName(id);
            HostStarId = hostStarId;
            OrbitIndex = orbitIndex;
            OrbitAngle = orbitAngle;
            OrbitRadius = orbitRadius;
        }

        public event Action<O_Planet> OnLifeDeveloped;
        public event Action<O_Planet> OnDestroyed;

        public void DevelopLife()
        {
            if (HasLife)
                return;

            HasLife = true;
            OnLifeDeveloped?.Invoke(this);
        }

        public void Destroy()
        {
            OnDestroyed?.Invoke(this);
        }
    }
}
