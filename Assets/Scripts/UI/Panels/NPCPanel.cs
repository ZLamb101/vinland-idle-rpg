using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NPCPanel : MonoBehaviour
{
    [Header("NPC Display")]
    public Image npcImage;
    public TextMeshProUGUI npcNameText;
    public Image questIndicator;
    
    [Header("Quest Indicator Sprites")]
    public Sprite questAvailableSprite;
    public Sprite questTurnInSprite;
    
    [Header("Interaction")]
    public Button interactButton;
    public Button buttonTemplate;
    public Transform buttonContainer;
    
    private NPCData npcData;
    private RectTransform rectTransform;
    private List<GameObject> activeButtons = new List<GameObject>();
    
    private class InteractionOption
    {
        public string text;
        public UnityEngine.Events.UnityAction action;
        public Sprite icon;
        
        public InteractionOption(string text, UnityEngine.Events.UnityAction action, Sprite icon = null)
        {
            this.text = text;
            this.action = action;
            this.icon = icon;
        }
    }
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (buttonTemplate != null)
        {
            buttonTemplate.gameObject.SetActive(false);
            if (buttonContainer == null)
            {
                buttonContainer = buttonTemplate.transform.parent;
            }
        }
        
        if (interactButton != null)
        {
            interactButton.onClick.AddListener(OnInteractButtonClicked);
        }
        
        EventBus.Subscribe<QuestAcceptedEvent>(OnQuestChanged);
        EventBus.Subscribe<QuestTurnedInEvent>(OnQuestChanged);
        EventBus.Subscribe<QuestCompletedEvent>(OnQuestChanged);
        EventBus.Subscribe<CharacterLoadedEvent>(OnCharacterChanged);
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<QuestAcceptedEvent>(OnQuestChanged);
        EventBus.Unsubscribe<QuestTurnedInEvent>(OnQuestChanged);
        EventBus.Unsubscribe<QuestCompletedEvent>(OnQuestChanged);
        EventBus.Unsubscribe<CharacterLoadedEvent>(OnCharacterChanged);
    }

    private void OnQuestChanged(GameEvent e)
    {
        if (npcData != null && gameObject.activeInHierarchy)
        {
            HideSideButtons();
            UpdateQuestIndicator();
        }
    }
    
    private void OnCharacterChanged(CharacterLoadedEvent e)
    {
        if (npcData != null && gameObject.activeInHierarchy)
        {
            HideSideButtons();
            UpdateQuestIndicator();
        }
    }
    
    public void Initialize(NPCData npc, Vector2 position)
    {
        npcData = npc;
        
        if (npc == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
        }
        
        if (npcImage != null)
        {
            npcImage.gameObject.SetActive(true);
            if (npc.npcSprite != null)
            {
                npcImage.sprite = npc.npcSprite;
            }
            
            RectTransform imageRect = npcImage.GetComponent<RectTransform>();
            if (imageRect != null)
            {
                Vector3 scale = imageRect.localScale;
                scale.x = npc.flipSprite ? -1f : 1f;
                imageRect.localScale = scale;
            }
        }
        
        if (npcNameText != null)
        {
            npcNameText.text = npc.npcName;
            npcNameText.gameObject.SetActive(true);
        }
        
        if (interactButton != null)
        {
            interactButton.gameObject.SetActive(true);
        }
        HideSideButtons();
        UpdateQuestIndicator();
        
        gameObject.SetActive(true);
    }
    
    void OnInteractButtonClicked()
    {
        if (npcData == null) return;
        
        List<InteractionOption> options = GetAvailableInteractions();
        
        if (options.Count == 0)
        {
            Debug.LogWarning($"[NPCPanel] No interactions available for {npcData.npcName}");
            return;
        }
        else if (options.Count == 1)
        {
            options[0].action?.Invoke();
        }
        else
        {
            ShowSideButtons(options);
        }
    }
    
    List<InteractionOption> GetAvailableInteractions()
    {
        List<InteractionOption> options = new List<InteractionOption>();
        
        if (npcData == null) return options;
        
        Services.TryGet<IQuestService>(out IQuestService questService);
        
        if (questService != null)
        {
            List<PlayerQuest> activeQuests = questService.GetActiveQuests();
            HashSet<string> processedQuestIds = new HashSet<string>();

            if (activeQuests != null)
            {
                foreach (var pQuest in activeQuests)
                {
                    QuestData qData = questService.GetQuestData(pQuest.questId);
                    
                    if (qData != null && IsQuestTurnInTarget(qData) && questService.CanTurnInQuest(qData))
                    {
                        QuestData capturedQuest = qData;
                        NPCData capturedNPC = npcData;
                        options.Add(new InteractionOption(
                            $"? {qData.questName}", 
                            () => { questService.ShowQuestTurnIn(capturedQuest, capturedNPC); HideSideButtons(); }, 
                            questTurnInSprite
                        ));
                        processedQuestIds.Add(qData.id);
                    }
                }
            }
            
            if (npcData.availableQuests != null)
            {
                foreach (var quest in npcData.availableQuests)
                {
                    if (questService.CanAcceptQuest(quest))
                    {
                        QuestData capturedQuest = quest;
                        NPCData capturedNPC = npcData;
                        options.Add(new InteractionOption(
                            $"! {quest.questName}", 
                            () => { questService.ShowQuestOffer(capturedQuest, capturedNPC); HideSideButtons(); }, 
                            questAvailableSprite
                        ));
                    }
                }
            }
        }
        
        if (npcData.npcType == NPCType.ShopNPC)
        {
            options.Add(new InteractionOption("Shop", () => { OnShopClicked(); HideSideButtons(); }, null));
        }
        
        if (npcData.npcType == NPCType.TalkableNPC)
        {
            options.Add(new InteractionOption("Talk", () => { OnTalkClicked(); HideSideButtons(); }, null));
        }
        else if (npcData.npcType == NPCType.ShopNPC && options.Count > 1)
        {
            options.Add(new InteractionOption("Talk", () => { OnTalkClicked(); HideSideButtons(); }, null));
        }
        
        return options;
    }
    
    private bool IsQuestTurnInTarget(QuestData quest)
    {
        if (quest.turnInNPC == npcData) return true;
        if (quest.turnInNPC == null && npcData.availableQuests != null && 
            npcData.availableQuests.Contains(quest)) return true;
        return false;
    }
    
    void ShowSideButtons(List<InteractionOption> options)
    {
        HideSideButtons();
        
        if (buttonTemplate == null || buttonContainer == null) return;
        
        foreach (var option in options)
        {
            CreateButton(option.text, option.action, option.icon);
        }
    }
    
    void HideSideButtons()
    {
        foreach (var btn in activeButtons)
        {
            if (btn != null)
            {
                Destroy(btn);
            }
        }
        activeButtons.Clear();
    }
    
    void CreateButton(string text, UnityEngine.Events.UnityAction action, Sprite icon)
    {
        GameObject btnObj = Instantiate(buttonTemplate.gameObject, buttonContainer);
        btnObj.SetActive(true);
        
        TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = text;
        
        Image iconImage = btnObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }
        
        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
        
        activeButtons.Add(btnObj);
    }
    
    void OnTalkClicked()
    {
        if (npcData == null) return;
        if (Services.TryGet<IDialogueService>(out var dialogueService))
        {
            dialogueService.StartDialogue(npcData);
        }
    }
    
    void OnShopClicked()
    {
        if (npcData == null || npcData.shopData == null) return;
        if (Services.TryGet<IShopService>(out var shopService))
        {
            shopService.OpenShop(npcData.shopData);
        }
    }
    
    void UpdateQuestIndicator()
    {
        if (questIndicator == null || npcData == null)
        {
            return;
        }
        
        if (!Services.TryGet<IQuestService>(out IQuestService questService))
        {
            questIndicator.gameObject.SetActive(false);
            return;
        }
        
        bool hasQuestToTurnIn = false;
        bool hasQuestAvailable = false;
        
        List<PlayerQuest> activeQuests = questService.GetActiveQuests();
        if (activeQuests != null)
        {
            foreach (var pQuest in activeQuests)
            {
                QuestData qData = questService.GetQuestData(pQuest.questId);
                if (qData != null && IsQuestTurnInTarget(qData) && questService.CanTurnInQuest(qData))
                {
                    hasQuestToTurnIn = true;
                    break;
                }
            }
        }
        
        if (!hasQuestToTurnIn && npcData.availableQuests != null)
        {
            foreach (var quest in npcData.availableQuests)
            {
                if (questService.CanAcceptQuest(quest))
                {
                    hasQuestAvailable = true;
                    break;
                }
            }
        }
        
        if (hasQuestToTurnIn && questTurnInSprite != null)
        {
            questIndicator.sprite = questTurnInSprite;
            questIndicator.gameObject.SetActive(true);
        }
        else if (hasQuestAvailable && questAvailableSprite != null)
        {
            questIndicator.sprite = questAvailableSprite;
            questIndicator.gameObject.SetActive(true);
        }
        else
        {
            questIndicator.gameObject.SetActive(false);
        }
    }
}
