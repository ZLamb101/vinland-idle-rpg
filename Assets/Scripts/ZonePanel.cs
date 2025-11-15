using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI panel that displays current zone information and handles navigation.
/// </summary>
public class ZonePanel : MonoBehaviour
{
    [Header("Zone Display")]
    public TextMeshProUGUI zoneNameText;
    public Image backgroundImage; // Background image for the current zone

    [Header("Navigation")]
    public Button previousZoneButton;
    public Button nextZoneButton;
    public TextMeshProUGUI previousZoneText;
    public TextMeshProUGUI nextZoneText;

    [Header("Quest Display")]
    public Button questIconButton; // Button to toggle quest action panel
    public GameObject questActionPanel; // The existing quest board container
    public GameObject questPanelPrefab; // Prefab for creating quest panels dynamically

    [Header("Monster Display")]
    [Tooltip("Container GameObject for monster panels. Should be an empty GameObject with RectTransform. Create as child of ZonePanel.")]
    public Transform monsterContainer; // Container for monster panels (parent RectTransform)
    public GameObject monsterPanelPrefab; // Prefab for creating monster panels dynamically

    [Header("NPC Display")]
    [Tooltip("Container GameObject for NPC panels. Should be an empty GameObject with RectTransform. Create as child of ZonePanel.")]
    public Transform npcContainer; // Container for NPC panels (parent RectTransform)
    public GameObject npcPanelPrefab; // Prefab for creating NPC panels dynamically
    
    [Header("Resource Gathering")]
    [Tooltip("Container GameObject for resource panels. Should be an empty GameObject with RectTransform. Create as child of ZonePanel.")]
    public Transform resourceContainer; // Container for resource panels (parent RectTransform)
    public GameObject resourcePanelPrefab; // Prefab for creating resource panels dynamically
    
    private List<GameObject> currentNPCPanels = new List<GameObject>(); // Track spawned NPC panels
    private List<GameObject> currentMonsterPanels = new List<GameObject>(); // Track spawned monster panels
    private List<GameObject> currentResourcePanels = new List<GameObject>(); // Track spawned resource panels

    private IZoneService zoneService;

    void Start()
    {
        //Get Zone Service - if it doesn't exist, create ZoneManager
        if (!Services.TryGet<IZoneService>(out zoneService))
        {
            Debug.LogWarning("[ZonePanel] ZoneService not found, creating ZoneManager...");
            GameObject zoneManagerObj = new GameObject("ZoneManager");
            ZoneManager zoneManager = zoneManagerObj.AddComponent<ZoneManager>();
            // ZoneManager.Awake() will register itself with Services
            
            // Try again after a frame to let Awake() run
            StartCoroutine(WaitForZoneManager());
            return; // Exit early, WaitForZoneManager will reinitialize everything
        }

        // Subscribe to zone changes
        if (zoneService != null)
        {
            zoneService.OnZoneChanged += OnZoneChanged;
            zoneService.OnQuestsChanged += OnQuestsChanged;
            
            Debug.Log("[ZonePanel] Subscribed to zone events");
        }
        else
        {
            // Try to initialize display anyway (ZoneManager might set currentZone in its Start)
            StartCoroutine(WaitForZoneManager());
        }
        
        // Setup navigation buttons
        if (previousZoneButton != null)
            previousZoneButton.onClick.AddListener(GoToPreviousZone);

        if (nextZoneButton != null)
            nextZoneButton.onClick.AddListener(GoToNextZone);

        // Setup quest icon button
        if (questIconButton != null)
            questIconButton.onClick.AddListener(ToggleQuestZone);
        
        // Resource gathering is now handled by ResourcePanel components
        
        UpdateNavigationButtons();
        InitializeQuestPanel();
        
        // Initialize quests for current zone when scene loads
        if (zoneService != null)
        {
            ZoneData currentZone = zoneService.GetCurrentZone();
            if (currentZone != null)
            {
                Debug.Log($"[ZonePanel] Current zone already loaded: {currentZone.zoneName}");
                InitializeQuestsForZone(currentZone);
            }
            else
            {
                Debug.Log("[ZonePanel] No current zone yet, will wait for OnZoneChanged event");
            }
        }
        
        // Update display after everything is initialized
        // This will display the zone if it's already loaded, or wait for OnZoneChanged if not
        UpdateZoneDisplay();
        
        Debug.Log("[ZonePanel] Start() completed");
    }

    System.Collections.IEnumerator WaitForZoneManager()
    {
        // Wait a frame for ZoneManager to initialize
        yield return null;

        // Try to get the service again
        if (Services.TryGet<IZoneService>(out zoneService))
        {
            Debug.Log("[ZonePanel] ZoneService found after waiting");
            
            zoneService.OnZoneChanged += OnZoneChanged;
            zoneService.OnQuestsChanged += OnQuestsChanged;
            
            // Setup navigation buttons
            if (previousZoneButton != null)
                previousZoneButton.onClick.AddListener(GoToPreviousZone);

            if (nextZoneButton != null)
                nextZoneButton.onClick.AddListener(GoToNextZone);

            // Setup quest icon button
            if (questIconButton != null)
                questIconButton.onClick.AddListener(ToggleQuestZone);
            
            UpdateNavigationButtons();
            InitializeQuestPanel();
            
            // Initialize quests for current zone
            ZoneData currentZone = zoneService.GetCurrentZone();
            if (currentZone != null)
            {
                Debug.Log($"[ZonePanel] Current zone after waiting: {currentZone.zoneName}");
                InitializeQuestsForZone(currentZone);
            }
            
            UpdateZoneDisplay();
        }
        else
        {
            Debug.LogError("[ZonePanel] ZoneService still not found after waiting!");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (zoneService != null)
        {
            zoneService.OnZoneChanged -= OnZoneChanged;
            zoneService.OnQuestsChanged -= OnQuestsChanged;
        }

        // Resource gathering events are now handled by ResourcePanel components
    }

    void OnZoneChanged(ZoneData zone)
    {
        Debug.Log($"[ZonePanel] OnZoneChanged event fired: {(zone != null ? zone.zoneName : "null")}");
        
        // Stop gathering when switching zones
        if (Services.TryGet<IResourceService>(out var resourceService) && resourceService.IsGathering())
        {
            resourceService.StopGathering();
        }
        
        // Close shop when switching zones
        if (Services.TryGet<IShopService>(out var shopService) && shopService.IsShopOpen())
        {
            shopService.CloseShop();
        }

        UpdateZoneDisplay();
        UpdateNavigationButtons();
        InitializeQuestsForZone(zone);
    }

    void OnQuestsChanged(QuestData[] quests)
    {
        // Quest loading is handled by the existing questActionPanel
        // No need to duplicate quest display logic here
    }

    void UpdateZoneDisplay()
    {
        if (zoneService == null)
        {
            Debug.Log("[ZonePanel] UpdateZoneDisplay() - zoneService is null");
            return;
        }

        ZoneData currentZone = zoneService.GetCurrentZone();
        if (currentZone == null)
        {
            Debug.Log("[ZonePanel] UpdateZoneDisplay() - currentZone is null");
            return;
        }

        Debug.Log($"[ZonePanel] UpdateZoneDisplay() - Displaying zone: {currentZone.zoneName}");

        // Update zone info
        if (zoneNameText != null)
            zoneNameText.text = currentZone.zoneName;

        // Update background image
        if (backgroundImage != null)
        {
            if (currentZone.backgroundImage != null)
            {
                backgroundImage.sprite = currentZone.backgroundImage;
                backgroundImage.gameObject.SetActive(true);
            }
            else
            {
                backgroundImage.gameObject.SetActive(false);
            }
        }

        // Update monster display - spawn monster panels dynamically
        Debug.Log("[ZonePanel] Updating monster display...");
        UpdateMonsterDisplay(currentZone);

        // Update resource display - spawn resource panels dynamically
        Debug.Log("[ZonePanel] Updating resource display...");
        UpdateResourceDisplay(currentZone);

        // Update NPC display - spawn NPC panels dynamically
        Debug.Log("[ZonePanel] Updating NPC display...");
        UpdateNPCDisplay(currentZone);
        
        Debug.Log("[ZonePanel] UpdateZoneDisplay() completed");
    }

    void UpdateNavigationButtons()
    {
        if (zoneService == null) return;

        // Update previous zone button
        bool canGoPrevious = zoneService.CanGoToPreviousZone();
        if (previousZoneButton != null)
        {
            previousZoneButton.gameObject.SetActive(canGoPrevious);
        }

        if (previousZoneText != null)
        {
            previousZoneText.gameObject.SetActive(canGoPrevious);
            if (canGoPrevious)
            {
                ZoneData previousZone = zoneService.GetPreviousZone();
                previousZoneText.text = $"← {previousZone.zoneName}";
            }
        }

        // Update next zone button
        bool canGoNext = zoneService.CanGoToNextZone();
        if (nextZoneButton != null)
        {
            nextZoneButton.interactable = canGoNext;
        }

        if (nextZoneText != null)
        {
            if (canGoNext)
            {
                ZoneData nextZone = zoneService.GetNextZone();
                nextZoneText.text = $"{nextZone.zoneName} →";
            }
            else
            {
                nextZoneText.text = "Next →";
            }
        }
    }


    void GoToPreviousZone()
    {
        if (zoneService != null)
        {
            zoneService.GoToPreviousZone();
        }
    }

    void GoToNextZone()
    {
        if (zoneService != null)
        {
            zoneService.GoToNextZone();
        }
    }

    void InitializeQuestPanel()
    {
        // Hide quest action panel initially
        if (questActionPanel != null)
        {
            questActionPanel.SetActive(false);
        }
    }

    void ToggleQuestZone()
    {
        if (questActionPanel != null)
        {
            bool isShowing = questActionPanel.activeSelf;
            questActionPanel.SetActive(!isShowing);
        }
    }

    void InitializeQuestsForZone(ZoneData zone)
    {
        if (zone == null || questActionPanel == null) return;

        // IMPORTANT: Clear static active quest reference when re-initializing
        // This prevents stale references from previous character sessions
        QuestPanel.ClearActiveQuestReference();

        var characterService = Services.Get<ICharacterService>();
        int playerLevel = characterService != null ? characterService.GetLevel() : 1;
        QuestData[] availableQuests = zone.GetAllQuests(); // Get all quests, including locked ones

        // Find QuestPanel components in the questActionPanel and update them
        QuestPanel[] questPanels = questActionPanel.GetComponentsInChildren<QuestPanel>(true); // Include inactive

        // If no QuestPanel components found, create them dynamically
        if (questPanels.Length == 0 && availableQuests.Length > 0)
        {
            CreateQuestPanels(availableQuests);
            questPanels = questActionPanel.GetComponentsInChildren<QuestPanel>(true);
        }

        // Update existing quest panels with new quest data
        for (int i = 0; i < questPanels.Length && i < availableQuests.Length; i++)
        {
            if (questPanels[i] != null && availableQuests[i] != null)
            {
                questPanels[i].SetQuest(availableQuests[i]);
            }
        }

        // Hide extra quest panels if we have more panels than quests
        for (int i = availableQuests.Length; i < questPanels.Length; i++)
        {
            if (questPanels[i] != null)
            {
                questPanels[i].gameObject.SetActive(false);
            }
        }

        // Show quest panels that have quests
        for (int i = 0; i < availableQuests.Length && i < questPanels.Length; i++)
        {
            if (questPanels[i] != null)
            {
                questPanels[i].gameObject.SetActive(true);
            }
        }

        // Hide the quest action panel after initializing quests
        if (questActionPanel != null)
        {
            questActionPanel.SetActive(false);
        }
    }

    void CreateQuestPanels(QuestData[] quests)
    {
        for (int i = 0; i < quests.Length; i++)
        {
            GameObject questPanelObj;

            if (questPanelPrefab != null)
            {
                questPanelObj = Instantiate(questPanelPrefab, questActionPanel.transform);
                questPanelObj.name = $"QuestPanel_{i}";

                // Set the quest data
                QuestPanel questPanel = questPanelObj.GetComponent<QuestPanel>();
                if (questPanel != null)
                {
                    questPanel.SetQuest(quests[i]);
                }
                else
                {
                }
            }
            else
            {
                // Fallback: create simple quest panel
                questPanelObj = new GameObject($"QuestPanel_{i}");
                questPanelObj.transform.SetParent(questActionPanel.transform);

                // Add QuestPanel component
                QuestPanel questPanel = questPanelObj.AddComponent<QuestPanel>();
                questPanel.SetQuest(quests[i]);
            }
        }
    }

    /// <summary>
    /// Update monster display by spawning/removing monster panels
    /// </summary>
    void UpdateMonsterDisplay(ZoneData zone)
    {
        if (zone == null) return;
        
        // Clear existing monster panels
        ClearMonsterPanels();
        
        // Get monsters from zone
        List<ZoneMonsterEntry> monsterEntries = zone.GetMonsterEntries();
        if (monsterEntries == null || monsterEntries.Count == 0)
        {
            return;
        }
        
        if (monsterContainer == null)
        {
            return;
        }
        
        if (monsterPanelPrefab == null)
        {
            return;
        }
        
        // Spawn monster panels for each monster entry
        foreach (ZoneMonsterEntry monsterEntry in monsterEntries)
        {
            if (monsterEntry.monster == null) continue;
            
            GameObject monsterPanelObj = Instantiate(monsterPanelPrefab, monsterContainer);
            MonsterPanel monsterPanel = monsterPanelObj.GetComponent<MonsterPanel>();
            
            if (monsterPanel != null)
            {
                monsterPanel.Initialize(monsterEntry.monster, monsterEntry.position);
                currentMonsterPanels.Add(monsterPanelObj);
            }
            else
            {
                Destroy(monsterPanelObj);
            }
        }
    }
    
    /// <summary>
    /// Clear all existing monster panels
    /// </summary>
    void ClearMonsterPanels()
    {
        foreach (GameObject panel in currentMonsterPanels)
        {
            if (panel != null)
            {
                Destroy(panel);
            }
        }
        currentMonsterPanels.Clear();
    }
    
    /// <summary>
    /// Update NPC display by spawning/removing NPC panels
    /// </summary>
    void UpdateNPCDisplay(ZoneData zone)
    {
        if (zone == null) return;
        
        // Clear existing NPC panels
        ClearNPCPanels();
        
        // Get NPCs from zone
        List<ZoneNPCEntry> npcs = zone.GetNPCs();
        if (npcs == null || npcs.Count == 0)
        {
            return;
        }
        
        if (npcContainer == null)
        {
            return;
        }
        
        if (npcPanelPrefab == null)
        {
            return;
        }
        
        // Spawn NPC panels for each NPC entry
        foreach (ZoneNPCEntry npcEntry in npcs)
        {
            if (npcEntry.npc == null) continue;
            
            GameObject npcPanelObj = Instantiate(npcPanelPrefab, npcContainer);
            NPCPanel npcPanel = npcPanelObj.GetComponent<NPCPanel>();
            
            if (npcPanel != null)
            {
                npcPanel.Initialize(npcEntry.npc, npcEntry.position);
                currentNPCPanels.Add(npcPanelObj);
            }
            else
            {
                Destroy(npcPanelObj);
            }
        }
    }
    
    /// <summary>
    /// Clear all existing NPC panels
    /// </summary>
    void ClearNPCPanels()
    {
        foreach (GameObject panel in currentNPCPanels)
        {
            if (panel != null)
            {
                Destroy(panel);
            }
        }
        currentNPCPanels.Clear();
    }
    
    /// <summary>
    /// Update resource display by spawning/removing resource panels
    /// </summary>
    void UpdateResourceDisplay(ZoneData zone)
    {
        if (zone == null) return;
        
        // Clear existing resource panels
        ClearResourcePanels();
        
        // Get resources from zone
        List<ZoneResourceEntry> resourceEntries = zone.GetResourceEntries();
        if (resourceEntries == null || resourceEntries.Count == 0)
        {
            return;
        }
        
        if (resourceContainer == null)
        {
            return;
        }
        
        if (resourcePanelPrefab == null)
        {
            return;
        }
        
        // Spawn resource panels for each resource entry
        foreach (ZoneResourceEntry resourceEntry in resourceEntries)
        {
            if (resourceEntry.resource == null) continue;
            
            GameObject resourcePanelObj = Instantiate(resourcePanelPrefab, resourceContainer);
            ResourcePanel resourcePanel = resourcePanelObj.GetComponent<ResourcePanel>();
            
            if (resourcePanel != null)
            {
                resourcePanel.Initialize(resourceEntry.resource, resourceEntry.position);
                currentResourcePanels.Add(resourcePanelObj);
            }
            else
            {
                Destroy(resourcePanelObj);
            }
        }
    }
    
    /// <summary>
    /// Clear all existing resource panels
    /// </summary>
    void ClearResourcePanels()
    {
        foreach (GameObject panel in currentResourcePanels)
        {
            if (panel != null)
            {
                Destroy(panel);
            }
        }
        currentResourcePanels.Clear();
    }
}