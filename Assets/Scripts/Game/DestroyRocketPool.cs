using System.Collections.Generic;
using UnityEngine;

namespace Universes.Game
{
    public class DestroyRocketPool : MonoBehaviour
    {
        [SerializeField] private List<DestroyRocketView> rockets = new();
        [SerializeField] private bool collectChildrenOnAwake = true;
        [SerializeField] private bool deactivateOnAwake = true;

        private readonly Queue<DestroyRocketView> _available = new();
        private readonly HashSet<DestroyRocketView> _availableSet = new();
        private readonly HashSet<DestroyRocketView> _inUse = new();

        public int AvailableCount => _available.Count;
        public int TotalCount => rockets.Count;

        private void Awake()
        {
            if (collectChildrenOnAwake)
                CollectChildRockets();

            RebuildPool();
        }

        public void CollectChildRockets()
        {
            rockets.Clear();
            rockets.AddRange(GetComponentsInChildren<DestroyRocketView>(true));
        }

        public void RebuildPool()
        {
            _available.Clear();
            _availableSet.Clear();
            _inUse.Clear();

            foreach (var rocket in rockets)
            {
                if (rocket == null)
                    continue;

                rocket.BindPool(this);
                if (deactivateOnAwake)
                    rocket.gameObject.SetActive(false);

                if (_availableSet.Add(rocket))
                    _available.Enqueue(rocket);
            }
        }

        public DestroyRocketView Rent()
        {
            while (_available.Count > 0)
            {
                var rocket = _available.Dequeue();
                _availableSet.Remove(rocket);
                if (rocket == null)
                    continue;

                _inUse.Add(rocket);
                rocket.gameObject.SetActive(true);
                return rocket;
            }

            return null;
        }

        public void Return(DestroyRocketView rocket)
        {
            if (rocket == null)
                return;

            _inUse.Remove(rocket);
            rocket.gameObject.SetActive(false);
            if (_availableSet.Add(rocket))
                _available.Enqueue(rocket);
        }

        public void ReturnAll()
        {
            foreach (var rocket in rockets)
            {
                if (rocket == null)
                    continue;

                rocket.StopAndReturnToPool();
            }
        }
    }
}
