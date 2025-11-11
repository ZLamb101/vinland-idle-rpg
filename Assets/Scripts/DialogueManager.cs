using System;
using UnityEngine;

/// <summary>
/// Singleton manager for handling NPC dialogue display and interactions.
/// </summary>
public class DialogueManager : MonoBehaviour, IDialogueService
{
    public static DialogueManager Instance { get; private set; }
    
    [Header("Dialogue State")]
    private NPCData currentNPC;
    private DialogueLine[] currentDialogueLines;
    private int currentDialogueIndex = 0;
    private bool isDialogueActive = false;
    
    // Events
    public event Action<NPCData> OnDialogueStarted; // When dialogue begins
    public event Action<string> OnDialogueTextChanged; // When dialogue text changes
    public event Action OnDialogueEnded; // When dialogue closes
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Register with service locator
        Services.Register<IDialogueService>(this);
    }
    
    void OnDestroy()
    {
        // Unregister from service locator
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
        
        OnDialogueStarted?.Invoke(npc);
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
        OnDialogueTextChanged?.Invoke(dialogueText);
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
        
        OnDialogueEnded?.Invoke();
    }
    
    // Getters
    public bool IsDialogueActive() => isDialogueActive;
    public NPCData GetCurrentNPC() => currentNPC;
    public bool HasMoreDialogue() => isDialogueActive && currentDialogueIndex < currentDialogueLines.Length - 1;
}

