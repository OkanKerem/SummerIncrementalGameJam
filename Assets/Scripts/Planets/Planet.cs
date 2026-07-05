using System;
using System.Collections.Generic;
using UnityEngine;

namespace Universes.Planets
{
    public class Planet
    {
        public int Id { get; }
        public int HostStarId { get; }
        public bool HasLife { get; set; }
        public bool IsUnstable { get; set; }
        public float OrbitAngle { get; }
        public float OrbitRadius { get; }
        public int OrbitIndex { get; }

        public Planet(int id, int hostStarId, int orbitIndex, float orbitAngle, float orbitRadius)
        {
            Id = id;
            HostStarId = hostStarId;
            OrbitIndex = orbitIndex;
            OrbitAngle = orbitAngle;
            OrbitRadius = orbitRadius;
        }

        public event Action<Planet> OnLifeDeveloped;
        public event Action<Planet> OnDestroyed;

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
