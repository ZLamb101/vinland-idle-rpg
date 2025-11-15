using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for displaying a single NPC panel with image, name, and interaction buttons.
/// This component is used as a prefab that can be instantiated multiple times.
/// </summary>
public class NPCPanel : MonoBehaviour
{
    [Header("NPC Display")]
    public Image npcImage; // Image showing the NPC sprite
    public TextMeshProUGUI npcNameText; // Text showing NPC name
    public Button interactButton; // Single button for all interactions
    
    private NPCData npcData;
    private RectTransform rectTransform;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Setup interact button
        if (interactButton != null)
            interactButton.onClick.AddListener(OnInteractClicked);
    }
    
    /// <summary>
    /// Initialize this NPC panel with NPC data and position
    /// </summary>
    public void Initialize(NPCData npc, Vector2 position)
    {
        npcData = npc;
        
        if (npc == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // Set position (use absolute pixel coordinates directly)
        if (rectTransform != null)
        {
            // Use position directly as anchored position (absolute pixel coordinates)
            rectTransform.anchoredPosition = position;
        }
        
        // Update NPC image
        if (npcImage != null)
        {
            npcImage.gameObject.SetActive(true);
            if (npc.npcSprite != null)
            {
                npcImage.sprite = npc.npcSprite;
            }
        }
        
        // Update NPC name text
        if (npcNameText != null)
        {
            npcNameText.text = npc.npcName;
            npcNameText.gameObject.SetActive(true);
        }
        
        // Show interact button for all NPCs
        if (interactButton != null)
        {
            interactButton.gameObject.SetActive(true);
            
            // Update button text based on NPC type
            TextMeshProUGUI buttonText = interactButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                switch (npc.npcType)
                {
                    case NPCType.ShopNPC:
                        buttonText.text = "Shop";
                        break;
                    case NPCType.TalkableNPC:
                    default:
                        buttonText.text = "Talk";
                        break;
                }
            }
        }
        
        gameObject.SetActive(true);
    }
    
    void OnInteractClicked()
    {
        if (npcData == null)
        {
            return;
        }
        
        // Route interaction based on NPC type
        switch (npcData.npcType)
        {
            case NPCType.ShopNPC:
                OnShopClicked();
                break;
            case NPCType.TalkableNPC:
            default:
                OnTalkClicked();
                break;
        }
    }
    
    void OnTalkClicked()
    {
        if (npcData == null)
        {
            return;
        }
        
        if (Services.TryGet<IDialogueService>(out var dialogueService))
        {
            dialogueService.StartDialogue(npcData);
        }
        else
        {
        }
    }
    
    void OnShopClicked()
    {
        if (npcData == null || npcData.npcType != NPCType.ShopNPC)
        {
            return;
        }
        
        if (npcData.shopData == null)
        {
            return;
        }
        
        var shopService = Services.Get<IShopService>();
        if (shopService != null)
        {
            shopService.OpenShop(npcData.shopData);
        }
        else
        {
        }
    }
}

