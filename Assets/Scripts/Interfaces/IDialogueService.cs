using System;

/// <summary>
/// Interface for dialogue management services.
/// Handles NPC dialogue display and interactions.
/// </summary>
public interface IDialogueService
{
    void StartDialogue(NPCData npc);
    void StartCustomDialogue(NPCData npc, string[] customLines);
    void StartCustomDialogueWithActions(NPCData npc, string[] customLines, System.Action onAccept, System.Action onDecline = null, string acceptButtonText = "Accept");
    void NextDialogue();
    void EndDialogue();
    
    bool IsDialogueActive();
    NPCData GetCurrentNPC();
    bool HasMoreDialogue();
}

