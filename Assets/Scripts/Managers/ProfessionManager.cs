using UnityEngine;

/// <summary>
/// Manager for character professions (Mining, Herbalism, Fishing, etc.)
/// Handles profession XP gains, level-ups, and saves per character.
/// </summary>
public class ProfessionManager : MonoBehaviour, IProfessionService
{
    private ProfessionData professionData;
    
    void Awake()
    {
        if (Services.IsRegistered<IProfessionService>())
        {
            Debug.LogWarning("[ProfessionManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        // Initialize profession data
        professionData = new ProfessionData();
        
        // Register with service locator
        Services.Register<IProfessionService>(this);
        
        Debug.Log("[ProfessionManager] Service registered and ready");
    }
    
    void OnDestroy()
    {
        Services.Unregister<IProfessionService>();
    }
    
    /// <summary>
    /// Add experience to a profession and handle level-ups
    /// </summary>
    public void AddProfessionXP(ProfessionType profession, int amount)
    {
        if (professionData == null)
        {
            Debug.LogWarning("[ProfessionManager] ProfessionData is null!");
            return;
        }
        
        int oldLevel = professionData.GetProfessionLevel(profession);
        int oldXP = professionData.GetProfessionXP(profession);
        
        // Add XP and get new level (handles level-ups internally)
        int newLevel = professionData.AddExperience(profession, amount);
        int newXP = professionData.GetProfessionXP(profession);
        int xpRequired = professionData.GetXPRequiredForNextLevel(profession);
        
        // Publish XP gained event
        EventBus.Publish(new ProfessionXPGainedEvent
        {
            profession = profession,
            xpGained = amount,
            currentXP = newXP,
            xpRequired = xpRequired
        });
        
        // If leveled up, publish level up event
        if (newLevel > oldLevel)
        {
            EventBus.Publish(new ProfessionLevelUpEvent
            {
                profession = profession,
                oldLevel = oldLevel,
                newLevel = newLevel
            });
            
            Debug.Log($"[ProfessionManager] {profession.GetDisplayName()} leveled up! {oldLevel} → {newLevel}");
        }
    }
    
    /// <summary>
    /// Get the current level of a profession
    /// </summary>
    public int GetProfessionLevel(ProfessionType profession)
    {
        if (professionData == null)
        {
            return 1;
        }
        return professionData.GetProfessionLevel(profession);
    }
    
    /// <summary>
    /// Get the current XP of a profession
    /// </summary>
    public int GetProfessionXP(ProfessionType profession)
    {
        if (professionData == null)
        {
            return 0;
        }
        return professionData.GetProfessionXP(profession);
    }
    
    /// <summary>
    /// Get XP required for next level
    /// </summary>
    public int GetProfessionXPRequired(ProfessionType profession)
    {
        if (professionData == null)
        {
            return 100;
        }
        return professionData.GetXPRequiredForNextLevel(profession);
    }
    
    /// <summary>
    /// Get the profession data (for saving)
    /// </summary>
    public ProfessionData GetProfessionData()
    {
        return professionData;
    }
    
    /// <summary>
    /// Load profession data (from save system)
    /// </summary>
    public void LoadProfessionData(ProfessionData data)
    {
        if (data != null)
        {
            professionData = data;
            Debug.Log("[ProfessionManager] Profession data loaded");
        }
        else
        {
            // Initialize new profession data if loading failed
            professionData = new ProfessionData();
            Debug.Log("[ProfessionManager] Initialized new profession data");
        }
    }
}


