using UnityEngine;

namespace Universes.Game
{
    public class SpaceStationView : MonoBehaviour
    {
        private enum StationState
        {
            Traveling,
            Orbiting
        }

        [SerializeField] private SpriteRenderer stationRenderer;
        [SerializeField] private Transform portraitRoot;
        [SerializeField] private float portraitOffsetY = 0.32f;
        [SerializeField] private float portraitScale = 0.18f;

        private PlanetManager _manager;
        private GameController _controller;
        private StationState _state;
        private int _planetId = -1;
        private float _orbitAngle;
        private float _orbitRadius;
        private float _orbitSpeed;
        private float _travelSpeed;
        private float _dnaTimer;
        private float _dnaInterval;
        private float _dnaAmount;
        private bool _preservePrefabVisuals;
        private Vector3 _travelStart;
        private float _travelDuration;
        private float _travelElapsed;

        public int PlanetId => _planetId;

        public void Initialize(PlanetManager manager, GameController controller, Planet sourcePlanet,
            Planet destinationPlanet, CivilizationBalanceConfig balance, Sprite[] stationSprites,
            GameObject alienPortraitPrefab, bool preservePrefabVisuals)
        {
            _manager = manager;
            _controller = controller;
            _preservePrefabVisuals = preservePrefabVisuals;
            _orbitSpeed = balance.spaceStationOrbitSpeed;
            _travelSpeed = balance.spaceStationTravelSpeed;
            _dnaInterval = balance.spaceStationDnaInterval;
            _dnaAmount = balance.spaceStationDnaPotentialPerTick;
            _dnaTimer = Random.Range(0f, _dnaInterval * 0.5f);

            if (preservePrefabVisuals)
                ResolveExistingVisuals();
            else
                EnsureVisuals();

            ApplyStationSprite(stationSprites);
            ConfigurePortrait(sourcePlanet, alienPortraitPrefab, preservePrefabVisuals);

            SetWorldPosition(_manager.GetPlanetWorldPosition(sourcePlanet));
            BeginTravelToPlanet(destinationPlanet);
        }

        private void Update()
        {
            if (_manager == null || _controller == null || _controller.IsRunEnded)
                return;

            if (_state == StationState.Traveling)
            {
                TickTravel();
                return;
            }

            var planet = _manager.GetPlanetById(_planetId);
            if (planet == null || !planet.IsAlive)
            {
                RetargetOrDestroy();
                return;
            }

            _orbitAngle += _orbitSpeed * Time.deltaTime;
            SetWorldPosition(GetOrbitWorldPosition());
            TickDna();
        }

        private void LateUpdate() => ApplyFixedWorldRotation();

        private void TickTravel()
        {
            var planet = _manager.GetPlanetById(_planetId);
            if (planet == null || !planet.IsAlive)
            {
                RetargetOrDestroy();
                return;
            }

            var end = GetOrbitWorldPosition();
            _travelElapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_travelElapsed / _travelDuration);
            var eased = t * t * (3f - 2f * t);
            SetWorldPosition(Vector3.Lerp(_travelStart, end, eased));

            if (t < 1f)
                return;

            _state = StationState.Orbiting;
            SetWorldPosition(GetOrbitWorldPosition());
            TickDna();
        }

        private void TickDna()
        {
            if (_dnaAmount <= 0f || _dnaInterval <= 0f)
                return;

            _dnaTimer -= Time.deltaTime;
            if (_dnaTimer > 0f)
                return;

            _dnaTimer = _dnaInterval;
            _controller.TrySpawnDnaPotential(transform.position, _dnaAmount * _controller.GetOrbitalDnaMultiplier());
        }

        private void RetargetOrDestroy()
        {
            var next = _manager.PickSpaceStationDestinationPlanet(_planetId);
            if (next == null)
            {
                _manager.UnregisterSpaceStation(this);
                Destroy(gameObject);
                return;
            }

            BeginTravelToPlanet(next);
        }

        private void BeginTravelToPlanet(Planet destinationPlanet)
        {
            if (destinationPlanet == null)
                return;

            _planetId = destinationPlanet.Id;
            _orbitRadius = _manager.GetSpaceStationOrbitRadius(destinationPlanet);
            _orbitAngle = Random.Range(0f, 360f);
            _travelStart = transform.position;
            _travelElapsed = 0f;
            var end = GetOrbitWorldPosition();
            _travelDuration = Mathf.Max(0.1f, Vector3.Distance(_travelStart, end) / Mathf.Max(0.1f, _travelSpeed));
            _state = StationState.Traveling;
            ApplyFixedWorldRotation();
        }

        private Vector3 GetOrbitWorldPosition()
        {
            var center = _manager.GetPlanetWorldPosition(_planetId);
            var rad = _orbitAngle * Mathf.Deg2Rad;
            return center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * _orbitRadius;
        }

        private void SetWorldPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            ApplyFixedWorldRotation();
        }

        private void ApplyFixedWorldRotation()
        {
            transform.rotation = Quaternion.identity;
        }

        private void EnsureVisuals()
        {
            if (stationRenderer == null)
            {
                var spriteGo = new GameObject("StationSprite");
                spriteGo.transform.SetParent(transform, false);
                stationRenderer = spriteGo.AddComponent<SpriteRenderer>();
            }

            if (portraitRoot == null)
            {
                var portraitGo = new GameObject("AlienPortrait");
                portraitGo.transform.SetParent(transform, false);
                portraitGo.transform.localPosition = Vector3.up * portraitOffsetY;
                portraitGo.transform.localScale = Vector3.one * portraitScale;
                portraitRoot = portraitGo.transform;
            }
        }

        private void ResolveExistingVisuals()
        {
            if (stationRenderer == null)
                stationRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
        }

        private void ApplyStationSprite(Sprite[] stationSprites)
        {
            if (stationRenderer == null)
                return;

            var selected = SelectSprite(stationSprites);
            if (selected != null)
                stationRenderer.sprite = selected;

            if (!_preservePrefabVisuals)
            {
                stationRenderer.color = Color.white;
                stationRenderer.sortingOrder = 11;
            }
        }

        private void ConfigurePortrait(Planet planet, GameObject alienPortraitPrefab, bool preservePrefabVisuals)
        {
            if (planet == null || !planet.HasSpecies)
                return;

            if (preservePrefabVisuals)
            {
                var portrait = FindExistingPortraitRoot();
                if (portrait != null)
                    SpeciesPortraitPool.ConfigurePortrait(portrait.gameObject, planet);
                return;
            }

            if (portraitRoot == null || alienPortraitPrefab == null)
                return;

            foreach (Transform child in portraitRoot)
                Destroy(child.gameObject);

            var instance = Instantiate(alienPortraitPrefab, portraitRoot);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            SpeciesPortraitPool.ConfigurePortrait(instance, planet);
        }

        private Transform FindExistingPortraitRoot()
        {
            var searchRoot = portraitRoot != null ? portraitRoot : transform;
            var namedAlien = FindChildRecursive(searchRoot, "Alien");
            if (namedAlien != null)
                return namedAlien;

            foreach (Transform child in searchRoot)
            {
                if (child.Find("Faces") != null && child.Find("Eyes") != null)
                    return child;
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;

            foreach (Transform child in root)
            {
                if (child.name == childName || child.name.StartsWith("Alien"))
                    return child;

                var found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private Sprite SelectSprite(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0)
                return stationRenderer != null ? stationRenderer.sprite : null;

            for (var attempt = 0; attempt < sprites.Length; attempt++)
            {
                var sprite = sprites[Random.Range(0, sprites.Length)];
                if (sprite != null)
                    return sprite;
            }

            return stationRenderer != null ? stationRenderer.sprite : null;
        }
    }
}
