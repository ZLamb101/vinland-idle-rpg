using System;

/// <summary>
/// Interface for dialogue management services.
/// Handles NPC dialogue display and interactions.
/// </summary>
public interface IDialogueService
{
    // Events
    event Action<NPCData> OnDialogueStarted; // When dialogue begins
    event Action<string> OnDialogueTextChanged; // When dialogue text changes
    event Action OnDialogueEnded; // When dialogue closes
    
    // Dialogue Control
    void StartDialogue(NPCData npc);
    void NextDialogue();
    void EndDialogue();
    
    // State Getters
    bool IsDialogueActive();
    NPCData GetCurrentNPC();
    bool HasMoreDialogue();
}

