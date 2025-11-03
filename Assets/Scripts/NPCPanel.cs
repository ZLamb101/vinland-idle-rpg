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
    public Button talkButton; // Button to talk to NPC
    public Button shopButton; // Button to open shop (only shown if NPC has shop)
    
    private NPCData npcData;
    private RectTransform rectTransform;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Setup buttons
        if (talkButton != null)
            talkButton.onClick.AddListener(OnTalkClicked);
        
        if (shopButton != null)
            shopButton.onClick.AddListener(OnShopClicked);
    }
    
    /// <summary>
    /// Initialize this NPC panel with NPC data and position
    /// </summary>
    public void Initialize(NPCData npc, Vector2 position)
    {
        npcData = npc;
        
        if (npc == null)
        {
            Debug.LogWarning("NPCPanel: Cannot initialize with null NPC!");
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
        
        // Show/hide talk button
        if (talkButton != null)
        {
            talkButton.gameObject.SetActive(true);
        }
        
        // Show/hide shop button (only if NPC has shop)
        if (shopButton != null)
        {
            bool showShop = npc.hasShop;
            shopButton.gameObject.SetActive(showShop);
        }
        
        gameObject.SetActive(true);
    }
    
    void OnTalkClicked()
    {
        if (npcData == null)
        {
            Debug.LogWarning("NPCPanel: Cannot talk - NPC is null!");
            return;
        }
        
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(npcData);
        }
        else
        {
            Debug.LogWarning("NPCPanel: DialogueManager.Instance is null!");
        }
    }
    
    void OnShopClicked()
    {
        if (npcData == null || !npcData.hasShop)
        {
            Debug.LogWarning("NPCPanel: NPC does not have a shop!");
            return;
        }
        
        Debug.Log($"Opening shop for {npcData.npcName}");
        // TODO: Open shop UI when shop system is implemented
    }
}

