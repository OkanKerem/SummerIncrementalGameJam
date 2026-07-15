using UnityEngine;

namespace Universes.Game
{
    [CreateAssetMenu(fileName = "PlanetType", menuName = "Universes/Planet Type")]
    public class PlanetTypeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public PlanetType planetType = PlanetType.Rocky;
        public string displayName = "Rocky Planet";
        [TextArea] public string description;

        [Header("Gameplay")]
        public int baseClickValue = 3;
        [Range(0f, 1f)] public float habitability = 0.55f;
        [Range(0f, 1f)] public float dnaChance = 0.12f;
        public float maxDurability = 100f;
        public bool canCivilize = true;
        [Min(0f)] public float spawnWeight = 10f;

        [Header("Visuals")]
        public Sprite planetSprite;
        public Color planetColor = new(0.65f, 0.55f, 0.45f);
        public Color lifeTintColor = new(0.35f, 0.95f, 0.55f);
        public Color civilizationTintColor = new(1f, 0.85f, 0.35f);
        [Min(0.05f)] public float visualScale = 0.22f;
        public int sortingOrder = 5;

        [Header("Effects")]
        public ParticleSystem spawnEffectPrefab;
        public ParticleSystem clickEffectPrefab;
        public ParticleSystem destroyEffectPrefab;
        [Tooltip("Used when this planet is destroyed by colliding with a planet from another star system. Falls back to Destroy Effect Prefab if empty.")]
        public ParticleSystem crossStarCollisionDestroyEffectPrefab;
        public Color effectTint = Color.white;
        [Tooltip("Only this child ParticleSystem is tinted. Leave empty to use a PlanetEffectTintTarget marker on the prefab.")]
        public string effectTintChildName;
        [Tooltip("Planet visualScale at which effect prefab scale is 1x.")]
        [Min(0.01f)] public float effectReferenceVisualScale = 0.22f;
        [Min(0.05f)] public float effectScaleMultiplier = 1f;

        public string GetDisplayName() =>
            string.IsNullOrWhiteSpace(displayName) ? planetType.ToString() : displayName;

        public void PlayEffect(ParticleSystem prefab, Vector3 worldPosition, Transform parent) =>
            PlanetEffectPlayer.Play(this, prefab, worldPosition, parent);
    }
}
