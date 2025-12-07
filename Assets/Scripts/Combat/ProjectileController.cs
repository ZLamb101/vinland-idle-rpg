using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles projectile spawning and attack animations for combat.
/// Extracted from CombatSceneController for single responsibility.
/// </summary>
public class ProjectileController : MonoBehaviour
{
    [Header("References")]
    public RectTransform combatSceneContainer;
    public Transform projectilePool;
    public Projectile projectilePrefab;
    
    // Callbacks for getting positions (set by CombatSceneController)
    private System.Func<Vector2> getHeroPosition;
    private System.Func<int, Vector2> getEnemyPosition;
    private System.Func<int, EnemyVisual> getEnemyVisual;
    private HeroVisual heroVisual;
    
    // Class-based auto-attack configuration
    private List<AutoAttackConfig> autoAttackConfigs = new List<AutoAttackConfig>();
    private AutoAttackConfig currentAutoAttackConfig;
    
    /// <summary>
    /// Initialize the controller with required dependencies
    /// </summary>
    public void Initialize(
        RectTransform container,
        Transform pool,
        Projectile prefab,
        HeroVisual hero,
        System.Func<Vector2> heroPositionFunc,
        System.Func<int, Vector2> enemyPositionFunc,
        System.Func<int, EnemyVisual> enemyVisualFunc)
    {
        combatSceneContainer = container;
        projectilePool = pool;
        projectilePrefab = prefab;
        heroVisual = hero;
        getHeroPosition = heroPositionFunc;
        getEnemyPosition = enemyPositionFunc;
        getEnemyVisual = enemyVisualFunc;
    }
    
    /// <summary>
    /// Update hero visual reference (called when hero changes)
    /// </summary>
    public void SetHeroVisual(HeroVisual hero)
    {
        heroVisual = hero;
    }
    
    /// <summary>
    /// Set the available auto-attack configurations
    /// </summary>
    public void SetAutoAttackConfigs(AutoAttackConfig[] configs)
    {
        autoAttackConfigs.Clear();
        if (configs != null)
        {
            autoAttackConfigs.AddRange(configs);
        }
    }
    
    /// <summary>
    /// Select the auto-attack config for the current character class
    /// </summary>
    public void SetCharacterClass(CharacterClass characterClass)
    {
        currentAutoAttackConfig = null;
        
        foreach (var config in autoAttackConfigs)
        {
            if (config != null && config.characterClass == characterClass)
            {
                currentAutoAttackConfig = config;
                
                if (heroVisual != null && config.attackType == AutoAttackType.Projectile && config.projectilePrefab != null)
                {
                    heroVisual.projectilePrefab = config.projectilePrefab;
                }
                break;
            }
        }
        
        if (currentAutoAttackConfig == null && heroVisual != null && projectilePrefab != null)
        {
            heroVisual.projectilePrefab = projectilePrefab;
        }
    }
    
    /// <summary>
    /// Hero attacks - spawn attack visual based on class configuration
    /// </summary>
    public void HeroAttack(float damage, int targetIndex, System.Action<float, int> onHit)
    {
        if (heroVisual == null || getEnemyVisual == null)
        {
            onHit?.Invoke(damage, targetIndex);
            return;
        }
        
        EnemyVisual targetEnemy = getEnemyVisual(targetIndex);
        if (targetEnemy == null)
        {
            onHit?.Invoke(damage, targetIndex);
            return;
        }
        
        AutoAttackType attackType = currentAutoAttackConfig?.attackType ?? AutoAttackType.Projectile;
        
        if (attackType == AutoAttackType.Instant)
        {
            HeroInstantAttack(damage, targetIndex, targetEnemy, onHit);
        }
        else
        {
            HeroProjectileAttack(damage, targetIndex, targetEnemy, onHit);
        }
    }
    
    /// <summary>
    /// Perform an instant attack (e.g. Warrior slash)
    /// </summary>
    void HeroInstantAttack(float damage, int targetIndex, EnemyVisual targetEnemy, System.Action<float, int> onHit)
    {
        if (heroVisual.gameObject.activeInHierarchy)
        {
            StartCoroutine(heroVisual.PlayAttackAnimation());
        }
        
        if (currentAutoAttackConfig?.instantEffectPrefab != null && targetEnemy.rectTransform != null)
        {
            Vector2 effectPosition = GetLocalPosition(targetEnemy.rectTransform);
            SpawnEffectAtPosition(
                currentAutoAttackConfig.instantEffectPrefab, 
                effectPosition, 
                currentAutoAttackConfig.instantEffectDuration
            );
        }
        
        onHit?.Invoke(damage, targetIndex);
    }
    
    /// <summary>
    /// Perform a projectile attack (e.g. Hunter arrow, Mage orb)
    /// </summary>
    void HeroProjectileAttack(float damage, int targetIndex, EnemyVisual targetEnemy, System.Action<float, int> onHit)
    {
        if (heroVisual.projectilePrefab == null)
        {
            if (currentAutoAttackConfig?.projectilePrefab != null)
            {
                heroVisual.projectilePrefab = currentAutoAttackConfig.projectilePrefab;
            }
            else if (projectilePrefab != null)
            {
                heroVisual.projectilePrefab = projectilePrefab;
            }
            else
            {
                onHit?.Invoke(damage, targetIndex);
                return;
            }
        }
        
        Vector2 targetPos = targetEnemy.GetPosition();
        
        heroVisual.Attack(
            targetPos,
            damage,
            (projectile) => {
                if (projectilePool != null && projectile.transform.parent != projectilePool)
                {
                    projectile.transform.SetParent(projectilePool);
                }
                
                float dealtDamage = projectile.GetDamage();
                onHit?.Invoke(dealtDamage, targetIndex);
                Destroy(projectile.gameObject);
            },
            (projectile) => {
                Destroy(projectile.gameObject);
            }
        );
    }
    
    /// <summary>
    /// Spawn a skill projectile that travels to target and triggers callback on hit
    /// </summary>
    public void SpawnSkillProjectile(SkillData skill, int targetIndex, float damage, System.Action<float, int> onHit)
    {
        if (heroVisual == null || getEnemyVisual == null)
        {
            onHit?.Invoke(damage, targetIndex);
            return;
        }
        
        EnemyVisual targetEnemy = getEnemyVisual(targetIndex);
        if (targetEnemy == null)
        {
            onHit?.Invoke(damage, targetIndex);
            return;
        }
        
        GameObject prefabToUse = skill.projectilePrefab != null ? skill.projectilePrefab : (projectilePrefab != null ? projectilePrefab.gameObject : null);
        
        if (prefabToUse == null)
        {
            onHit?.Invoke(damage, targetIndex);
            return;
        }
        
        Vector2 startPos = GetLocalPosition(heroVisual.rectTransform);
        
        Vector2 targetPos = GetLocalPosition(targetEnemy.rectTransform);
        
        GameObject projectileObj = Instantiate(prefabToUse, combatSceneContainer);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        
        if (projectile == null)
        {
            Destroy(projectileObj);
            onHit?.Invoke(damage, targetIndex);
            return;
        }
        
        projectile.ApplySkillVisuals(skill);
        
        // Launch projectile
        projectile.Launch(
            startPos,
            targetPos,
            damage,
            (proj) => {
                if (projectilePool != null && proj.transform.parent != projectilePool)
                {
                    proj.transform.SetParent(projectilePool);
                }
                float dealtDamage = proj.GetDamage();
                onHit?.Invoke(dealtDamage, targetIndex);
                Destroy(proj.gameObject);
            },
            (proj) => {
                Destroy(proj.gameObject);
            }
        );
    }
    
    /// <summary>
    /// Spawn a skill projectile from an enemy toward the hero
    /// </summary>
    public void SpawnEnemySkillProjectile(SkillData skill, int enemyIndex, float damage, System.Action<float> onHit)
    {
        if (heroVisual == null || getEnemyVisual == null)
        {
            onHit?.Invoke(damage);
            return;
        }
        
        EnemyVisual sourceEnemy = getEnemyVisual(enemyIndex);
        if (sourceEnemy == null)
        {
            onHit?.Invoke(damage);
            return;
        }
        
        // Determine which projectile prefab to use
        GameObject prefabToUse = skill.projectilePrefab != null ? skill.projectilePrefab : (projectilePrefab != null ? projectilePrefab.gameObject : null);
        
        if (prefabToUse == null)
        {
            onHit?.Invoke(damage);
            return;
        }
        
        // Get enemy position
        Vector2 startPos = GetLocalPosition(sourceEnemy.rectTransform);
        
        // Get hero position
        Vector2 targetPos = GetLocalPosition(heroVisual.rectTransform);
        
        // Instantiate projectile
        GameObject projectileObj = Instantiate(prefabToUse, combatSceneContainer);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        
        if (projectile == null)
        {
            Destroy(projectileObj);
            onHit?.Invoke(damage);
            return;
        }
        
        // Apply skill visuals
        projectile.ApplySkillVisuals(skill);
        
        // Launch projectile from enemy to hero
        projectile.Launch(
            startPos,
            targetPos,
            damage,
            (proj) => {
                if (projectilePool != null && proj.transform.parent != projectilePool)
                {
                    proj.transform.SetParent(projectilePool);
                }
                float dealtDamage = proj.GetDamage();
                onHit?.Invoke(dealtDamage);
                Destroy(proj.gameObject);
            },
            (proj) => {
                Destroy(proj.gameObject);
            }
        );
    }
    
    /// <summary>
    /// Enemy attacks - play swipe animation for specific enemy
    /// </summary>
    public void EnemyAttack(int enemyIndex, System.Action onComplete)
    {
        if (getEnemyVisual == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        EnemyVisual enemy = getEnemyVisual(enemyIndex);
        if (enemy == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        enemy.PerformAttack(onComplete);
    }
    /// <summary>
    /// Spawn a hit effect at the target's position (for instant skills)
    /// </summary>
    public void SpawnHitEffect(SkillData skill, int targetIndex)
    {
        if (skill.hitEffectPrefab == null || combatSceneContainer == null)
            return;
        
        Vector2 effectPosition = Vector2.zero;
        
        if (targetIndex >= 0 && getEnemyVisual != null)
        {
            // Effect on monster
            EnemyVisual enemy = getEnemyVisual(targetIndex);
            if (enemy != null && enemy.rectTransform != null)
            {
                effectPosition = GetLocalPosition(enemy.rectTransform);
            }
            else
            {
                return;
            }
        }
        else if (targetIndex == -1 && heroVisual != null && heroVisual.rectTransform != null)
        {
            // Effect on player (self-buff)
            effectPosition = GetLocalPosition(heroVisual.rectTransform);
        }
        else
        {
            return;
        }
        
        SpawnEffectAtPosition(skill.hitEffectPrefab, effectPosition, skill.hitEffectDuration);
    }
    
    /// <summary>
    /// Spawn a hit effect on the hero (for instant enemy skills)
    /// </summary>
    public void SpawnHitEffectOnHero(SkillData skill)
    {
        // Delegate to SpawnHitEffect with targetIndex -1 (player)
        SpawnHitEffect(skill, -1);
    }
    
    /// <summary>
    /// Helper to spawn an effect prefab at a position
    /// </summary>
    void SpawnEffectAtPosition(GameObject prefab, Vector2 position, float duration)
    {
        GameObject effectObj = Instantiate(prefab, combatSceneContainer);
        
        RectTransform effectRect = effectObj.GetComponent<RectTransform>();
        if (effectRect != null)
        {
            effectRect.anchoredPosition = position;
        }
        else
        {
            effectObj.transform.localPosition = new Vector3(position.x, position.y, 0f);
        }
        
        // Destroy effect after duration (default 2 seconds)
        float effectDuration = duration > 0f ? duration : 2f;
        Destroy(effectObj, effectDuration);
    }
    
    /// <summary>
    /// Get local position of a RectTransform relative to combatSceneContainer
    /// Returns the CENTER of the rect, adjusting for pivot
    /// </summary>
    Vector2 GetLocalPosition(RectTransform rectTransform)
    {
        if (rectTransform == null || combatSceneContainer == null)
            return Vector2.zero;
        
        Vector2 basePos;
        
        if (rectTransform.parent == combatSceneContainer)
        {
            basePos = rectTransform.anchoredPosition;
        }
        else
        {
            Vector3 worldPos = rectTransform.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                combatSceneContainer,
                RectTransformUtility.WorldToScreenPoint(null, worldPos),
                null,
                out basePos
            );
        }
        
        // Adjust for pivot to get center of the sprite
        // If pivot.y is 0 (bottom), offset up by half the height
        // If pivot.y is 0.5 (center), no offset needed
        // If pivot.y is 1 (top), offset down by half the height
        float pivotOffsetY = (0.5f - rectTransform.pivot.y) * rectTransform.rect.height;
        float pivotOffsetX = (0.5f - rectTransform.pivot.x) * rectTransform.rect.width;
        
        return new Vector2(basePos.x + pivotOffsetX, basePos.y + pivotOffsetY);
    }
    
    /// <summary>
    /// Clean up projectile pool
    /// </summary>
    public void Cleanup()
    {
        if (projectilePool != null)
        {
            foreach (Transform child in projectilePool)
            {
                Destroy(child.gameObject);
            }
        }
    }
}

