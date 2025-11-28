using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bootstrap script that initializes all persistent managers before loading the first scene.
/// This ensures all managers exist with DontDestroyOnLoad before any scene needs them.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The first scene to load after bootstrap completes")]
    [SerializeField] private string firstSceneName = "CharacterScene";
    
    [Header("Debug")]
    [Tooltip("Show detailed logs during initialization")]
    [SerializeField] private bool showDebugLogs = true;
    
    void Awake()
    {
        Log("═══════════════════════════════════════");
        Log("       Game Bootstrap Started");
        Log("═══════════════════════════════════════");
        
        // Create all persistent managers
        // Order matters for dependencies!
        
        // Config must load first - other managers may use config values
        CreateManager<ConfigManager>();
        
        // Core gameplay managers
        CreateManager<CharacterManager>();
        CreateManager<CharacterSessionManager>();
        CreateManager<CombatManager>();
        CreateManager<ZoneManager>();
        CreateManager<ResourceManager>();
        CreateManager<QuestManager>();
        
        // Equipment and progression
        CreateManager<EquipmentManager>();
        CreateManager<TalentManager>();
        CreateManager<ProfessionManager>();
        CreateManager<CraftingManager>();
        CreateManager<ShopManager>();
        CreateManager<BankManager>();
        
        // Skills
        CreateManager<SkillManager>();
        
        // Activities
        CreateManager<AwayActivityManager>();
        
        // UI and utilities
        CreateManager<DialogueManager>();
        
        Log("═══════════════════════════════════════");
        Log($"All {15} managers created successfully!");
        Log($"Loading first scene: {firstSceneName}");
        Log("═══════════════════════════════════════");
        
        // Load the actual first scene
        // All managers will persist due to DontDestroyOnLoad
        SceneManager.LoadScene(firstSceneName);
    }
    
    /// <summary>
    /// Create a manager if it doesn't already exist.
    /// Manager's Awake() will handle singleton pattern, DontDestroyOnLoad, and service registration.
    /// </summary>
    private void CreateManager<T>(string customName = null) where T : MonoBehaviour
    {
        string managerName = customName ?? typeof(T).Name;
        
        // Check if manager already exists (shouldn't happen, but safe)
        T existing = FindAnyObjectByType<T>();
        if (existing != null)
        {
            Log($"⚠️  {managerName} already exists, skipping creation");
            return;
        }
        
        // Create new GameObject with manager component
        GameObject managerObj = new GameObject(managerName);
        T manager = managerObj.AddComponent<T>();
        
        Log($"✓ Created {managerName}");
    }
    
    /// <summary>
    /// Log message with Bootstrap prefix if debug logging is enabled
    /// </summary>
    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Bootstrap] {message}");
        }
    }
}

