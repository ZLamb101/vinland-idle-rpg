using System;
using UnityEngine;

/// <summary>
/// Singleton manager for handling NPC dialogue display and interactions.
/// </summary>
public class DialogueManager : MonoBehaviour
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
    }
    
    /// <summary>
    /// Start a dialogue with an NPC
    /// </summary>
    public void StartDialogue(NPCData npc)
    {
        if (npc == null)
        {
            Debug.LogWarning("DialogueManager: Cannot start dialogue - NPC is null!");
            return;
        }
        
        if (npc.dialogueLines == null || npc.dialogueLines.Length == 0)
        {
            Debug.LogWarning($"DialogueManager: NPC {npc.npcName} has no dialogue lines!");
            return;
        }
        
        currentNPC = npc;
        currentDialogueLines = npc.dialogueLines;
        currentDialogueIndex = 0;
        isDialogueActive = true;
        
        OnDialogueStarted?.Invoke(npc);
        ShowCurrentDialogueLine();
        
        Debug.Log($"Started dialogue with {npc.npcName}");
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
        
        Debug.Log("Dialogue ended");
    }
    
    // Getters
    public bool IsDialogueActive() => isDialogueActive;
    public NPCData GetCurrentNPC() => currentNPC;
    public bool HasMoreDialogue() => isDialogueActive && currentDialogueIndex < currentDialogueLines.Length - 1;
}

