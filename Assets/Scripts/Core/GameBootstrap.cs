using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bootstrap script that initializes all persistent managers before loading the first scene.
/// This ensures all managers exist with DontDestroyOnLoad before any scene needs them.
/// 
/// SETUP:
/// 1. Create a new scene called "Bootstrap.unity"
/// 2. Add this script to an empty GameObject
/// 3. In Build Settings, make Bootstrap scene index 0 (first scene)
/// 4. Set firstSceneName to your actual first scene
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
        
        // Core gameplay managers
        CreateManager<CharacterManager>();
        CreateManager<CombatManager>();
        CreateManager<ZoneManager>();
        CreateManager<ResourceManager>();
        
        // Equipment and progression
        CreateManager<EquipmentManager>();
        CreateManager<TalentManager>();
        CreateManager<ShopManager>();
        
        // Activities
        CreateManager<AwayActivityManager>();
        
        // UI and utilities
        CreateManager<DialogueManager>();
        
        // Note: GameLog is NOT created here - it's a UI component that should
        // exist in scenes where the log UI is needed. It will register itself.
        
        Log("═══════════════════════════════════════");
        Log($"All {9} managers created successfully!");
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
        T existing = FindObjectOfType<T>();
        if (existing != null)
        {
            Log($"⚠️  {managerName} already exists, skipping creation");
            return;
        }
        
        // Create new GameObject with manager component
        GameObject managerObj = new GameObject(managerName);
        T manager = managerObj.AddComponent<T>();
        
        // Manager's Awake() will automatically:
        // - Handle singleton pattern (prevent duplicates)
        // - Call DontDestroyOnLoad(gameObject)
        // - Register with Services.Register<IService>(this)
        
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

