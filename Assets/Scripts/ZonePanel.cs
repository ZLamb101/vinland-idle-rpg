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

    void Start()
    {
        // Subscribe to zone changes
        if (ZoneManager.Instance != null)
        {
            ZoneManager.Instance.OnZoneChanged += OnZoneChanged;
            ZoneManager.Instance.OnQuestsChanged += OnQuestsChanged;
        }
        else
        {
            Debug.LogWarning("ZonePanel: ZoneManager.Instance is null in Start! Ensure ZoneManager is initialized before ZonePanel.");
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
        
        // Update display after everything is initialized
        UpdateZoneDisplay();
    }

    System.Collections.IEnumerator WaitForZoneManager()
    {
        // Wait a frame for ZoneManager to initialize
        yield return null;

        if (ZoneManager.Instance != null)
        {
            ZoneManager.Instance.OnZoneChanged += OnZoneChanged;
            ZoneManager.Instance.OnQuestsChanged += OnQuestsChanged;
            UpdateZoneDisplay();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (ZoneManager.Instance != null)
        {
            ZoneManager.Instance.OnZoneChanged -= OnZoneChanged;
            ZoneManager.Instance.OnQuestsChanged -= OnQuestsChanged;
        }

        // Resource gathering events are now handled by ResourcePanel components
    }

    void OnZoneChanged(ZoneData zone)
    {
        // Stop gathering when switching zones
        if (ResourceManager.Instance != null && ResourceManager.Instance.IsGathering())
        {
            ResourceManager.Instance.StopGathering();
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
        if (ZoneManager.Instance == null)
        {
            Debug.LogWarning("ZonePanel: ZoneManager.Instance is null!");
            return;
        }

        ZoneData currentZone = ZoneManager.Instance.GetCurrentZone();
        if (currentZone == null)
        {
            Debug.LogWarning("ZonePanel: Current zone is null!");
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
    }

    void UpdateNavigationButtons()
    {
        if (ZoneManager.Instance == null) return;

        // Update previous zone button
        bool canGoPrevious = ZoneManager.Instance.CanGoToPreviousZone();
        if (previousZoneButton != null)
        {
            previousZoneButton.gameObject.SetActive(canGoPrevious);
        }

        if (previousZoneText != null)
        {
            previousZoneText.gameObject.SetActive(canGoPrevious);
            if (canGoPrevious)
            {
                ZoneData previousZone = ZoneManager.Instance.GetPreviousZone();
                previousZoneText.text = $"← {previousZone.zoneName}";
            }
        }

        // Update next zone button
        bool canGoNext = ZoneManager.Instance.CanGoToNextZone();
        if (nextZoneButton != null)
        {
            nextZoneButton.interactable = canGoNext;
        }

        if (nextZoneText != null)
        {
            if (canGoNext)
            {
                ZoneData nextZone = ZoneManager.Instance.GetNextZone();
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
        if (ZoneManager.Instance != null)
        {
            ZoneManager.Instance.GoToPreviousZone();
        }
    }

    void GoToNextZone()
    {
        if (ZoneManager.Instance != null)
        {
            ZoneManager.Instance.GoToNextZone();
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

        int playerLevel = CharacterManager.Instance != null ? CharacterManager.Instance.GetLevel() : 1;
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
                    Debug.LogError($"QuestPanel component not found on prefab!");
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
            Debug.Log($"ZonePanel: Zone {zone.zoneName} has no monsters");
            return;
        }
        
        if (monsterContainer == null)
        {
            Debug.LogWarning("ZonePanel: monsterContainer is not assigned! Cannot spawn monster panels.");
            return;
        }
        
        if (monsterPanelPrefab == null)
        {
            Debug.LogWarning("ZonePanel: monsterPanelPrefab is not assigned! Cannot spawn monster panels.");
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
                Debug.Log($"ZonePanel: Spawned monster panel for {monsterEntry.monster.monsterName} at position {monsterEntry.position}");
            }
            else
            {
                Debug.LogError("ZonePanel: MonsterPanel component not found on prefab!");
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
            Debug.Log($"ZonePanel: Zone {zone.zoneName} has no NPCs");
            return;
        }
        
        if (npcContainer == null)
        {
            Debug.LogWarning("ZonePanel: npcContainer is not assigned! Cannot spawn NPC panels.");
            return;
        }
        
        if (npcPanelPrefab == null)
        {
            Debug.LogWarning("ZonePanel: npcPanelPrefab is not assigned! Cannot spawn NPC panels.");
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
                Debug.Log($"ZonePanel: Spawned NPC panel for {npcEntry.npc.npcName} at position {npcEntry.position}");
            }
            else
            {
                Debug.LogError("ZonePanel: NPCPanel component not found on prefab!");
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
            Debug.Log($"ZonePanel: Zone {zone.zoneName} has no resources");
            return;
        }
        
        if (resourceContainer == null)
        {
            Debug.LogWarning("ZonePanel: resourceContainer is not assigned! Cannot spawn resource panels.");
            return;
        }
        
        if (resourcePanelPrefab == null)
        {
            Debug.LogWarning("ZonePanel: resourcePanelPrefab is not assigned! Cannot spawn resource panels.");
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
                Debug.Log($"ZonePanel: Spawned resource panel for {resourceEntry.resource.resourceName} at position {resourceEntry.position}");
            }
            else
            {
                Debug.LogError("ZonePanel: ResourcePanel component not found on prefab!");
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