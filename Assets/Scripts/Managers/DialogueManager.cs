using System;
using UnityEngine;

/// <summary>
/// Singleton manager for handling NPC dialogue display and interactions.
/// </summary>
public class DialogueManager : MonoBehaviour, IDialogueService
{
    
    [Header("Dialogue State")]
    private NPCData currentNPC;
    private DialogueLine[] currentDialogueLines;
    private int currentDialogueIndex = 0;
    private bool isDialogueActive = false;
    
    // Events migrated to EventBus - see GameEvent.cs for event types
    
    void Awake()
    {
        if (Services.IsRegistered<IDialogueService>())
        {
            Debug.LogWarning("[DialogueManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        // Register with service locator
        Services.Register<IDialogueService>(this);
    }
    
    void OnDestroy()
    {
        Services.Unregister<IDialogueService>();
    }
    
    /// <summary>
    /// Start a dialogue with an NPC
    /// </summary>
    public void StartDialogue(NPCData npc)
    {
        if (npc == null)
        {
            return;
        }
        
        if (npc.dialogueLines == null || npc.dialogueLines.Length == 0)
        {
            return;
        }
        
        // Close shop if open
        var shopService = Services.Get<IShopService>();
        if (shopService != null && shopService.IsShopOpen())
        {
            shopService.CloseShop();
        }
        
        currentNPC = npc;
        currentDialogueLines = npc.dialogueLines;
        currentDialogueIndex = 0;
        isDialogueActive = true;
        
        EventBus.Publish(new DialogueStartedEvent { npc = npc });
        ShowCurrentDialogueLine();
    }
    
    /// <summary>
    /// Show the current dialogue line
    /// </summary>
    void ShowCurrentDialogueLine()
    {
        if (currentDialogueLines == null || currentDialogueIndex < 0 || currentDialogueIndex >= currentDialogueLines.Length)
        {
            EndDialogue();
            return;
        }
        
        string dialogueText = currentDialogueLines[currentDialogueIndex].text;
        EventBus.Publish(new DialogueTextChangedEvent { dialogueText = dialogueText });
    }
    
    /// <summary>
    /// Advance to the next dialogue line
    /// </summary>
    public void NextDialogue()
    {
        if (!isDialogueActive) return;
        
        currentDialogueIndex++;
        
        if (currentDialogueIndex >= currentDialogueLines.Length)
        {
            EndDialogue();
        }
        else
        {
            ShowCurrentDialogueLine();
        }
    }
    
    /// <summary>
    /// End the current dialogue
    /// </summary>
    public void EndDialogue()
    {
        if (!isDialogueActive) return;
        
        isDialogueActive = false;
        currentNPC = null;
        currentDialogueLines = null;
        currentDialogueIndex = 0;
        
        EventBus.Publish(new DialogueEndedEvent());
    }
    
    // Getters
    public bool IsDialogueActive() => isDialogueActive;
    public NPCData GetCurrentNPC() => currentNPC;
    public bool HasMoreDialogue() => isDialogueActive && currentDialogueIndex < currentDialogueLines.Length - 1;
}

