using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI panel that displays current zone information and handles navigation.
/// </summary>
public class ZonePanel : MonoBehaviour
{
    [Header("Zone Display")]
    public TextMeshProUGUI zoneNameText;
    public Image monsterIconImage; // Image showing the first monster's icon
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

    [Header("Combat")]
    public Button fightButton; // Button to start combat

    [Header("NPC Display")]
    public Image npcImage; // Image showing the NPC sprite
    public TextMeshProUGUI npcNameText; // Text showing NPC name
    public Button talkButton; // Button to talk to NPC
    public Button shopButton; // Button to open shop (only shown if NPC has shop)

    [Header("Resource Gathering")]
    public Image resourceIconImage; // Image showing the resource icon
    public Button resourceGatherButton; // Button to start/stop gathering
    public TextMeshProUGUI resourceGatherButtonText; // Text on the gather button
    public Slider resourceProgressSlider; // Progress bar for gathering
    public TextMeshProUGUI resourceDetailsText; // Shows resource name and gather rate

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

        // Setup fight button
        if (fightButton != null)
            fightButton.onClick.AddListener(ToggleCombat);
        
        // Setup NPC buttons
        if (talkButton != null)
            talkButton.onClick.AddListener(OnTalkClicked);
        
        if (shopButton != null)
            shopButton.onClick.AddListener(OnShopClicked);
        
        // Setup resource gather button
        if (resourceGatherButton != null)
            resourceGatherButton.onClick.AddListener(ToggleResourceGathering);
        
        // Subscribe to ResourceManager events
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnGatheringStateChanged += OnGatheringStateChanged;
            ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
            ResourceManager.Instance.OnGatherProgressChanged += OnGatherProgressChanged;
        }
        else
        {
            Debug.LogWarning("ZonePanel: ResourceManager.Instance is null! Make sure ResourceManager exists in scene.");
            StartCoroutine(WaitForResourceManager());
        }
        
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

    System.Collections.IEnumerator WaitForResourceManager()
    {
        // Wait a frame for ResourceManager to initialize
        yield return null;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnGatheringStateChanged += OnGatheringStateChanged;
            ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
            ResourceManager.Instance.OnGatherProgressChanged += OnGatherProgressChanged;
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

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnGatheringStateChanged -= OnGatheringStateChanged;
            ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
            ResourceManager.Instance.OnGatherProgressChanged -= OnGatherProgressChanged;
        }
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

        // Update monster icon and fight button visibility
        MonsterData[] monsters = currentZone.GetMonsters();
        
        // Filter out null entries from the array
        System.Collections.Generic.List<MonsterData> validMonsters = new System.Collections.Generic.List<MonsterData>();
        if (monsters != null)
        {
            foreach (MonsterData monster in monsters)
            {
                if (monster != null)
                {
                    validMonsters.Add(monster);
                }
            }
        }
        
        bool hasMonsters = validMonsters.Count > 0;
        
        Debug.Log($"ZonePanel: Zone {currentZone.zoneName} has {validMonsters.Count} valid monsters (total array length: {monsters?.Length ?? 0})");
        
        // Show/hide monster icon
        if (monsterIconImage != null)
        {
            monsterIconImage.gameObject.SetActive(hasMonsters);
            
            // Set the icon to the first monster's sprite if available
            if (hasMonsters && validMonsters[0] != null && validMonsters[0].monsterSprite != null)
            {
                monsterIconImage.sprite = validMonsters[0].monsterSprite;
                Debug.Log($"ZonePanel: Set monster icon to {validMonsters[0].monsterName}");
            }
            else if (hasMonsters)
            {
                Debug.LogWarning($"ZonePanel: First monster exists but has no sprite!");
            }
        }
        else
        {
            Debug.LogWarning("ZonePanel: monsterIconImage is not assigned!");
        }
        
        // Show/hide fight button
        if (fightButton != null)
        {
            fightButton.gameObject.SetActive(hasMonsters);
        }
        else
        {
            Debug.LogWarning("ZonePanel: fightButton is not assigned!");
        }

        // Update resource display
        ResourceData resource = currentZone.GetResource();
        bool hasResource = resource != null;
        
        Debug.Log($"ZonePanel: Zone {currentZone.zoneName} has resource: {hasResource} {(hasResource ? resource.resourceName : "")}");
        
        // Show/hide resource icon
        if (resourceIconImage != null)
        {
            resourceIconImage.gameObject.SetActive(hasResource);
            
            if (hasResource && resource.resourceIcon != null)
            {
                resourceIconImage.sprite = resource.resourceIcon;
                Debug.Log($"ZonePanel: Set resource icon to {resource.resourceName}");
            }
            else if (hasResource)
            {
                Debug.LogWarning($"ZonePanel: Resource exists but has no icon!");
            }
        }
        else
        {
            Debug.LogWarning("ZonePanel: resourceIconImage is not assigned!");
        }
        
        // Show/hide resource gather button
        if (resourceGatherButton != null)
        {
            resourceGatherButton.gameObject.SetActive(hasResource);
        }
        
        // Show/hide resource progress slider
        if (resourceProgressSlider != null)
        {
            resourceProgressSlider.gameObject.SetActive(hasResource);
            resourceProgressSlider.value = 0f;
        }
        
        // Update resource details text
        if (resourceDetailsText != null)
        {
            if (hasResource)
            {
                resourceDetailsText.text = $"{resource.resourceName}\n{resource.gatherRate:F1}/sec";
                resourceDetailsText.gameObject.SetActive(true);
            }
            else
            {
                resourceDetailsText.gameObject.SetActive(false);
            }
        }

        // Update NPC display
        NPCData npc = currentZone.GetNPC();
        bool hasNPC = npc != null;
        
        Debug.Log($"ZonePanel: Zone {currentZone.zoneName} has NPC: {hasNPC} {(hasNPC ? npc.npcName : "")}");

        // Show/hide NPC image
        if (npcImage != null)
        {
            npcImage.gameObject.SetActive(hasNPC);

            if (hasNPC && npc.npcSprite != null)
            {
                npcImage.sprite = npc.npcSprite;
            }
        }

        // Update NPC name text
        if (npcNameText != null)
        {
            if (hasNPC)
            {
                npcNameText.text = npc.npcName;
                npcNameText.gameObject.SetActive(true);
            }
            else
            {
                npcNameText.gameObject.SetActive(false);
            }
        }

        // Show/hide talk button
        if (talkButton != null)
        {
            talkButton.gameObject.SetActive(hasNPC);
        }

        // Show/hide shop button (only if NPC has shop)
        if (shopButton != null)
        {
            bool showShop = hasNPC && npc.hasShop;
            shopButton.gameObject.SetActive(showShop);
        }

        // Update gather button state
        UpdateGatherButtonState();
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

    void ToggleCombat()
    {
        Debug.Log("ToggleCombat called!");

        if (CombatManager.Instance == null)
        {
            Debug.LogError("CombatManager.Instance is NULL! Make sure CombatManager GameObject exists in scene!");
            return;
        }

        // Check if combat is already active
        if (CombatManager.Instance.GetCombatState() != CombatManager.CombatState.Idle)
        {
            // Combat is active - end it
            Debug.Log("Ending combat and hiding panel");
            CombatManager.Instance.EndCombat();
            return;
        }

        // Combat is not active - start it
        if (ZoneManager.Instance == null)
        {
            Debug.LogError("ZoneManager.Instance is NULL!");
            return;
        }

        ZoneData currentZone = ZoneManager.Instance.GetCurrentZone();
        if (currentZone == null)
        {
            Debug.LogWarning("No zone selected for combat!");
            return;
        }

        MonsterData[] monsters = currentZone.GetMonsters();
        if (monsters == null || monsters.Length == 0)
        {
            Debug.LogWarning($"Zone {currentZone.zoneName} has no monsters to fight!");
            return;
        }

        Debug.Log($"Starting combat with {monsters.Length} monsters from {currentZone.zoneName}");

        // Start combat with this zone's monsters
        CombatManager.Instance.StartCombat(monsters);
    }

    void OnTalkClicked()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance is NULL! Make sure DialogueManager exists in scene!");
            return;
        }

        ZoneData currentZone = ZoneManager.Instance?.GetCurrentZone();
        if (currentZone == null)
        {
            Debug.LogWarning("No zone selected for talking!");
            return;
        }

        NPCData npc = currentZone.GetNPC();
        if (npc == null)
        {
            Debug.LogWarning("No NPC in this zone!");
            return;
        }

        DialogueManager.Instance.StartDialogue(npc);
    }

    void OnShopClicked()
    {
        ZoneData currentZone = ZoneManager.Instance?.GetCurrentZone();
        if (currentZone == null)
        {
            Debug.LogWarning("No zone selected!");
            return;
        }

        NPCData npc = currentZone.GetNPC();
        if (npc == null || !npc.hasShop)
        {
            Debug.LogWarning("NPC does not have a shop!");
            return;
        }

        // TODO: Open shop UI when shop system is implemented
        Debug.Log($"Opening shop for {npc.npcName}");
    }

    void ToggleResourceGathering()
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogError("ResourceManager.Instance is NULL! Make sure ResourceManager exists in scene!");
            return;
        }

        ZoneData currentZone = ZoneManager.Instance?.GetCurrentZone();
        if (currentZone == null)
        {
            Debug.LogWarning("No zone selected for gathering!");
            return;
        }

        if (ResourceManager.Instance.IsGathering())
        {
            // Stop gathering
            ResourceManager.Instance.StopGathering();
        }
        else
        {
            // Start gathering
            ResourceManager.Instance.StartGathering(currentZone);
        }
    }

    void OnGatheringStateChanged(bool isGathering)
    {
        UpdateGatherButtonState();
    }

    void OnResourceChanged(ResourceData resource)
    {
        // Resource changed, update display
        if (resource != null && resourceIconImage != null && resource.resourceIcon != null)
        {
            resourceIconImage.sprite = resource.resourceIcon;
        }
    }

    void OnGatherProgressChanged(float progress)
    {
        if (resourceProgressSlider != null)
        {
            resourceProgressSlider.value = progress;
        }
    }

    void UpdateGatherButtonState()
    {
        if (resourceGatherButton == null || resourceGatherButtonText == null) return;

        bool isGathering = ResourceManager.Instance != null && ResourceManager.Instance.IsGathering();

        if (isGathering)
        {
            resourceGatherButtonText.text = "Stop Gather";
        }
        else
        {
            resourceGatherButtonText.text = "Gather";
        }
    }
}