using UnityEngine;
using UnityEngine.UI;

namespace Universes.Game
{
    public class SpeciesPortraitMount : MonoBehaviour
    {
        [SerializeField] private Transform portraitRoot;
        [SerializeField] private Image fallbackImage;

        private SpeciesPortraitPool _pool;
        private GameObject _instance;

        public void Bind(Planet planet, SpeciesPortraitPool pool)
        {
            Release();

            _pool = pool;
            if (portraitRoot == null)
                portraitRoot = transform;

            if (planet == null)
                return;

            if (_pool != null && _pool.HasPrefab)
                _instance = _pool.Acquire(portraitRoot, planet);

            if (fallbackImage != null)
            {
                fallbackImage.color = _pool != null && _pool.HasPrefab
                    ? new Color(1f, 1f, 1f, 0.01f)
                    : SpeciesPortraitPool.GetSpeciesColor(planet);
            }
        }

        public void Release()
        {
            if (_pool != null && _instance != null)
                _pool.Release(_instance);

            _instance = null;
        }
    }
}
