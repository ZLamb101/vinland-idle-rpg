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
    private System.Action pendingAcceptAction;
    private System.Action pendingDeclineAction;
    
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
    
    private void CloseShopIfOpen()
    {
        if (Services.TryGet<IShopService>(out var shopService) && shopService.IsShopOpen())
        {
            shopService.CloseShop();
        }
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
        
        CloseShopIfOpen();
        
        currentNPC = npc;
        currentDialogueLines = npc.dialogueLines;
        currentDialogueIndex = 0;
        isDialogueActive = true;
        
        EventBus.Publish(new DialogueStartedEvent { npc = npc });
        ShowCurrentDialogueLine();
    }
    
    /// <summary>
    /// Start a dialogue with custom text lines
    /// </summary>
    public void StartCustomDialogue(NPCData npc, string[] customLines)
    {
        if (npc == null || customLines == null || customLines.Length == 0)
        {
            return;
        }
        
        CloseShopIfOpen();
        
        currentNPC = npc;
        
        // Convert string array to DialogueLine array
        currentDialogueLines = new DialogueLine[customLines.Length];
        for (int i = 0; i < customLines.Length; i++)
        {
            currentDialogueLines[i] = new DialogueLine { text = customLines[i] };
        }
        
        currentDialogueIndex = 0;
        isDialogueActive = true;
        
        EventBus.Publish(new DialogueStartedEvent { npc = npc });
        ShowCurrentDialogueLine();
    }
    
    public void StartCustomDialogueWithActions(NPCData npc, string[] customLines, System.Action onAccept, System.Action onDecline = null, string acceptButtonText = "Accept")
    {
        if (npc == null || customLines == null || customLines.Length == 0)
        {
            return;
        }
        
        CloseShopIfOpen();
        
        currentNPC = npc;
        
        currentDialogueLines = new DialogueLine[customLines.Length];
        for (int i = 0; i < customLines.Length; i++)
        {
            currentDialogueLines[i] = new DialogueLine { text = customLines[i] };
        }
        
        currentDialogueIndex = 0;
        isDialogueActive = true;
        pendingAcceptAction = onAccept;
        pendingDeclineAction = onDecline;
        
        EventBus.Publish(new DialogueWithActionsStartedEvent 
        { 
            npc = npc,
            hasActions = true,
            acceptButtonText = acceptButtonText
        });
        ShowCurrentDialogueLine();
    }
    
    public void ExecuteAcceptAction()
    {
        if (pendingAcceptAction != null)
        {
            pendingAcceptAction.Invoke();
            pendingAcceptAction = null;
            pendingDeclineAction = null;
        }
        EndDialogue();
    }
    
    public void ExecuteDeclineAction()
    {
        if (pendingDeclineAction != null)
        {
            pendingDeclineAction.Invoke();
        }
        pendingAcceptAction = null;
        pendingDeclineAction = null;
        EndDialogue();
    }
    
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
        pendingAcceptAction = null;
        pendingDeclineAction = null;
        
        EventBus.Publish(new DialogueEndedEvent());
    }
    
    // Getters
    public bool IsDialogueActive() => isDialogueActive;
    public NPCData GetCurrentNPC() => currentNPC;
    public bool HasMoreDialogue() => isDialogueActive && currentDialogueIndex < currentDialogueLines.Length - 1;
}

