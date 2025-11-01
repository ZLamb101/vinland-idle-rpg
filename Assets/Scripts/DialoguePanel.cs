using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel that displays NPC dialogue.
/// Shows dialogue text and handles advancing through dialogue lines.
/// </summary>
public class DialoguePanel : MonoBehaviour
{
    [Header("Dialogue Panel")]
    public GameObject dialoguePanel; // The main panel to show/hide
    
    [Header("Dialogue Display")]
    public TextMeshProUGUI dialogueText; // Dialogue text display
    
    [Header("Controls")]
    public Button nextButton; // Button to advance dialogue
    public Button closeButton; // Button to close dialogue
    
    void Start()
    {
        // Subscribe to dialogue events
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted += OnDialogueStarted;
            DialogueManager.Instance.OnDialogueTextChanged += OnDialogueTextChanged;
            DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded;
        }
        
        // Setup buttons
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
            // Hide close button initially - nextButton handles both Next and Close
            closeButton.gameObject.SetActive(false);
        }
        
        // Hide panel initially
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted -= OnDialogueStarted;
            DialogueManager.Instance.OnDialogueTextChanged -= OnDialogueTextChanged;
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded;
        }
    }
    
    void OnDialogueStarted(NPCData npc)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        
        // Hide the separate close button - nextButton handles both Next and Close
        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
        
        // Update next button visibility
        UpdateNextButtonVisibility();
    }
    
    void OnDialogueTextChanged(string text)
    {
        if (dialogueText != null)
            dialogueText.text = text;
        
        UpdateNextButtonVisibility();
    }
    
    void OnDialogueEnded()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        
        // Re-enable close button when dialogue ends (if you want it for other purposes)
        // For now, we'll keep it hidden since nextButton handles everything
    }
    
    void UpdateNextButtonVisibility()
    {
        if (nextButton != null && DialogueManager.Instance != null)
        {
            // Always show the next button (it will handle Next/Close based on state)
            nextButton.gameObject.SetActive(true);
            
            // Update button text if it has a TextMeshProUGUI component
            TextMeshProUGUI buttonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = DialogueManager.Instance.HasMoreDialogue() ? "Next" : "Close";
            }
        }
    }
    
    void OnNextClicked()
    {
        if (DialogueManager.Instance != null)
        {
            if (DialogueManager.Instance.HasMoreDialogue())
            {
                DialogueManager.Instance.NextDialogue();
            }
            else
            {
                DialogueManager.Instance.EndDialogue();
            }
        }
    }
    
    void OnCloseClicked()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.EndDialogue();
        }
    }
}

