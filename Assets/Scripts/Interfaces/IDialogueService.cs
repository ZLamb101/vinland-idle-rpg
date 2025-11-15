using System;

/// <summary>
/// Interface for dialogue management services.
/// Handles NPC dialogue display and interactions.
/// </summary>
public interface IDialogueService
{
    // Events migrated to EventBus - see GameEvent.cs
    
    // Dialogue Control
    void StartDialogue(NPCData npc);
    void NextDialogue();
    void EndDialogue();
    
    // State Getters
    bool IsDialogueActive();
    NPCData GetCurrentNPC();
    bool HasMoreDialogue();
}

