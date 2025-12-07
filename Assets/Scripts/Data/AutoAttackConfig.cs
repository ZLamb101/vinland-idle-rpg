using UnityEngine;

/// <summary>
/// Defines the type of auto-attack visual.
/// </summary>
public enum AutoAttackType
{
    Projectile,  // Arrow, orb, etc. that travels to target
    Instant      // Instant effect that appears on target (slash, etc.)
}

/// <summary>
/// Configuration for class-specific combat visuals.
/// Create one asset per class and assign to CombatSceneController.
/// Consolidates hero sprite, auto-attack type, and effect prefabs.
/// </summary>
[CreateAssetMenu(fileName = "ClassCombat_NewClass", menuName = "Vinland/Combat/Class Combat Config")]
public class AutoAttackConfig : ScriptableObject
{
    [Header("Class Association")]
    [Tooltip("Which character class uses this combat configuration")]
    public CharacterClass characterClass;
    
    [Header("Hero Visual")]
    [Tooltip("Sprite to display for the hero in combat (optional - uses default if not set)")]
    public Sprite heroSprite;
    
    [Header("Attack Type")]
    [Tooltip("Projectile = travels to target, Instant = appears on target immediately")]
    public AutoAttackType attackType = AutoAttackType.Projectile;
    
    [Header("Projectile Settings")]
    [Tooltip("Projectile prefab for Projectile attack type (arrows, orbs, etc.)")]
    public Projectile projectilePrefab;
    
    [Header("Instant Effect Settings")]
    [Tooltip("Effect prefab for Instant attack type (slash animation, etc.)")]
    public GameObject instantEffectPrefab;
    
    [Tooltip("How long the instant effect lasts before being destroyed")]
    public float instantEffectDuration = 0.5f;
}
