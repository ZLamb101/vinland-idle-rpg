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
    
    [Header("Zone Connections")]
    [Tooltip("Container GameObject for zone connection buttons. Should be an empty GameObject with RectTransform. Create as child of ZonePanel.")]
    public Transform connectionContainer; // Container for connection buttons (parent RectTransform)
    public GameObject connectionButtonPrefab; // Prefab for creating connection buttons dynamically
    
    private List<GameObject> currentNPCPanels = new List<GameObject>(); // Track spawned NPC panels
    private List<GameObject> currentMonsterPanels = new List<GameObject>(); // Track spawned monster panels
    private List<GameObject> currentResourcePanels = new List<GameObject>(); // Track spawned resource panels
    private List<GameObject> currentConnectionButtons = new List<GameObject>(); // Track spawned connection buttons

    private IZoneService zoneService;

    void Start()
    {
        // Get Zone Service - should always exist due to Bootstrap scene
        if (!Services.TryGet<IZoneService>(out zoneService))
        {
            Debug.LogError("[ZonePanel] ZoneService not found! Did Bootstrap scene run? Check Build Settings.");
            return;
        }

        // Subscribe to zone changes via EventBus
        EventBus.Subscribe<ZoneChangedEvent>(OnZoneChanged);
        EventBus.Subscribe<QuestsUpdatedEvent>(OnQuestsChanged);
        
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
                InitializeQuestsForZone(currentZone);
            }
        }
        
        // Update display after everything is initialized
        // This will display the zone if it's already loaded, or wait for OnZoneChanged if not
        UpdateZoneDisplay();
    }

    System.Collections.IEnumerator WaitForZoneManager()
    {
        // Wait a frame for ZoneManager to initialize
        yield return null;

        // Try to get the service again
        if (Services.TryGet<IZoneService>(out zoneService))
        {
            EventBus.Subscribe<ZoneChangedEvent>(OnZoneChanged);
            EventBus.Subscribe<QuestsUpdatedEvent>(OnQuestsChanged);
            
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
        // Unsubscribe from EventBus
        EventBus.Unsubscribe<ZoneChangedEvent>(OnZoneChanged);
        EventBus.Unsubscribe<QuestsUpdatedEvent>(OnQuestsChanged);

        // Resource gathering events are now handled by ResourcePanel components
    }

    void OnZoneChanged(ZoneChangedEvent e)
    {
        ZoneData zone = e.newZone;
        
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

    void OnQuestsChanged(QuestsUpdatedEvent e)
    {
        // Quest loading is handled by the existing questActionPanel
        // No need to duplicate quest display logic here
    }

    void UpdateZoneDisplay()
    {
        if (zoneService == null)
        {
            return;
        }

        ZoneData currentZone = zoneService.GetCurrentZone();
        if (currentZone == null)
        {
            return;
        }

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
        UpdateMonsterDisplay(currentZone);

        // Update resource display - spawn resource panels dynamically
        UpdateResourceDisplay(currentZone);

        // Update NPC display - spawn NPC panels dynamically
        UpdateNPCDisplay(currentZone);
        
        // Update zone connection buttons - spawn connection buttons dynamically
        UpdateConnectionDisplay(currentZone);
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
        ZoneData nextZone = zoneService.GetNextZone();
        bool hasNextZone = nextZone != null;
        
        if (nextZoneButton != null)
        {
            nextZoneButton.gameObject.SetActive(hasNextZone);
            if (hasNextZone)
            {
                nextZoneButton.interactable = canGoNext;
            }
        }

        if (nextZoneText != null)
        {
            nextZoneText.gameObject.SetActive(hasNextZone);
            if (hasNextZone)
            {
                nextZoneText.text = $"{nextZone.zoneName} →";
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
    
    /// <summary>
    /// Update zone connection display by spawning/removing connection buttons
    /// </summary>
    void UpdateConnectionDisplay(ZoneData zone)
    {
        if (zone == null) return;
        
        // Clear existing connection buttons
        ClearConnectionButtons();
        
        // Get extra connections from zone
        List<ZoneConnection> connections = zone.extraConnections;
        if (connections == null || connections.Count == 0)
        {
            return;
        }
        
        if (connectionContainer == null)
        {
            return;
        }
        
        if (connectionButtonPrefab == null)
        {
            return;
        }
        
        // Spawn connection buttons for each extra connection
        for (int i = 0; i < connections.Count; i++)
        {
            ZoneConnection connection = connections[i];
            if (connection == null || connection.targetZone == null) continue;
            
            GameObject connectionButtonObj = Instantiate(connectionButtonPrefab, connectionContainer);
            ZoneConnectionButton connectionButton = connectionButtonObj.GetComponent<ZoneConnectionButton>();
            
            if (connectionButton != null)
            {
                // Use the connection's position and scale from the data
                connectionButton.Initialize(connection, i, connection.position, connection.scale);
                currentConnectionButtons.Add(connectionButtonObj);
            }
            else
            {
                Debug.LogWarning("[ZonePanel] ConnectionButtonPrefab is missing ZoneConnectionButton component!");
                Destroy(connectionButtonObj);
            }
        }
    }
    
    /// <summary>
    /// Clear all existing connection buttons
    /// </summary>
    void ClearConnectionButtons()
    {
        foreach (GameObject button in currentConnectionButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        currentConnectionButtons.Clear();
    }
}