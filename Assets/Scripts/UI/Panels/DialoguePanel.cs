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
    public Button nextButton;
    public Button closeButton;
    public Button acceptButton;

    private IDialogueService dialogueService;
    private DialogueManager dialogueManager;
    private bool hasAcceptAction = false;
    private string acceptButtonText = "Accept";
    
    void Start()
    {
        if (Services.TryGet<IDialogueService>(out dialogueService))
        {
            dialogueManager = dialogueService as DialogueManager;
            EventBus.Subscribe<DialogueStartedEvent>(OnDialogueStarted);
            EventBus.Subscribe<DialogueWithActionsStartedEvent>(OnDialogueWithActionsStarted);
            EventBus.Subscribe<DialogueTextChangedEvent>(OnDialogueTextChanged);
            EventBus.Subscribe<DialogueEndedEvent>(OnDialogueEnded);
        }
        
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
            closeButton.gameObject.SetActive(false);
        }
        
        if (acceptButton != null)
        {
            acceptButton.onClick.AddListener(OnAcceptClicked);
            acceptButton.gameObject.SetActive(false);
        }
        
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<DialogueStartedEvent>(OnDialogueStarted);
        EventBus.Unsubscribe<DialogueWithActionsStartedEvent>(OnDialogueWithActionsStarted);
        EventBus.Unsubscribe<DialogueTextChangedEvent>(OnDialogueTextChanged);
        EventBus.Unsubscribe<DialogueEndedEvent>(OnDialogueEnded);
        
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseClicked);
        if (acceptButton != null)
            acceptButton.onClick.RemoveListener(OnAcceptClicked);
    }
    
    void OnDialogueStarted(DialogueStartedEvent e)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        
        hasAcceptAction = false;
        
        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
        
        if (acceptButton != null)
            acceptButton.gameObject.SetActive(false);
        
        UpdateNextButtonVisibility();
    }
    
    void OnDialogueWithActionsStarted(DialogueWithActionsStartedEvent e)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        
        hasAcceptAction = true;
        acceptButtonText = e.acceptButtonText;
        
        if (acceptButton != null)
            acceptButton.gameObject.SetActive(false);
        
        UpdateNextButtonVisibility();
    }
    
    void OnDialogueTextChanged(DialogueTextChangedEvent e)
    {
        if (dialogueText != null)
            dialogueText.text = e.dialogueText;
        
        UpdateNextButtonVisibility();
        UpdateAcceptButtonVisibility();
    }
    
    void OnDialogueEnded(DialogueEndedEvent e)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        
        hasAcceptAction = false;
        
        if (acceptButton != null)
            acceptButton.gameObject.SetActive(false);
    }
    
    void UpdateNextButtonVisibility()
    {
        if (nextButton != null && Services.TryGet<IDialogueService>(out dialogueService))
        {
            nextButton.gameObject.SetActive(true);
            
            TextMeshProUGUI buttonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = dialogueService.HasMoreDialogue() ? "Next" : "Close";
            }
        }
    }
    
    void UpdateAcceptButtonVisibility()
    {
        if (acceptButton != null && Services.TryGet<IDialogueService>(out dialogueService))
        {
            bool isLastPage = !dialogueService.HasMoreDialogue();
            acceptButton.gameObject.SetActive(hasAcceptAction && isLastPage);
            
            if (hasAcceptAction && isLastPage)
            {
                TextMeshProUGUI buttonText = acceptButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = acceptButtonText;
                }
            }
        }
    }
    
    void OnNextClicked()
    {
        if (Services.TryGet<IDialogueService>(out dialogueService))
        {
            if (dialogueService.HasMoreDialogue())
            {
                dialogueService.NextDialogue();
            }
            else
            {
                if (hasAcceptAction && dialogueManager != null)
                {
                    dialogueManager.ExecuteDeclineAction();
                }
                else
                {
                    dialogueService.EndDialogue();
                }
            }
        }
    }
    
    void OnCloseClicked()
    {
        if (Services.TryGet<IDialogueService>(out dialogueService))
        {
            dialogueService.EndDialogue();
        }
    }
    
    void OnAcceptClicked()
    {
        if (dialogueManager != null)
        {
            dialogueManager.ExecuteAcceptAction();
        }
    }
}

