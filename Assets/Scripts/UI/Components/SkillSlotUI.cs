using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI component for a single skill slot in the action bar.
/// Supports drag-drop and displays cooldown overlay.
/// </summary>
public class SkillSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public Image skillIcon;
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;
    public TextMeshProUGUI manaCostText;
    public Image slotBackground;
    public Button slotButton;
    
    [Header("Slot Settings")]
    public int slotIndex = 0;
    
    [Header("Empty Slot")]
    public Sprite emptySlotSprite;
    public Color emptySlotColor = new Color(1f, 1f, 1f, 0.2f);
    
    [Header("Drag Settings")]
    public Canvas parentCanvas;
    
    // Current skill in this slot
    private SkillData currentSkill;
    private ISkillService skillService;
    
    // Drag state
    private bool isDragging = false;
    private GameObject dragIcon;
    private RectTransform dragIconRect;
    
    void Start()
    {
        skillService = Services.Get<ISkillService>();
        
        // Subscribe to action bar changes
        EventBus.Subscribe<ActionBarChangedEvent>(OnActionBarChanged);
        EventBus.Subscribe<SkillCooldownUpdatedEvent>(OnCooldownUpdated);
        
        // Find parent canvas if not set
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
        
        // Initialize display
        RefreshDisplay();
        
        // Setup button click
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<ActionBarChangedEvent>(OnActionBarChanged);
        EventBus.Unsubscribe<SkillCooldownUpdatedEvent>(OnCooldownUpdated);
    }
    
    void Update()
    {
        // Update cooldown display
        if (currentSkill != null && skillService != null)
        {
            float remaining = skillService.GetRemainingCooldown(currentSkill);
            float progress = skillService.GetCooldownProgress(currentSkill);
            
            // Check if skill is on its own cooldown
            if (remaining > 0f)
            {
                // Show skill-specific cooldown
                if (cooldownOverlay != null)
                {
                    cooldownOverlay.fillAmount = 1f - progress;
                    cooldownOverlay.gameObject.SetActive(true);
                }
                
                if (cooldownText != null)
                {
                    cooldownText.text = remaining.ToString("F1");
                    cooldownText.gameObject.SetActive(true);
                }
            }
            else
            {
                // Skill is ready - check if GCD is blocking it
                float gcdRemaining = skillService.GetGlobalCooldownRemaining();
                
                if (gcdRemaining > 0f)
                {
                    // Show GCD overlay
                    float gcdTotal = GameBalance.Combat.skillGlobalCooldown;
                    float gcdProgress = gcdRemaining / gcdTotal;
                    
                    if (cooldownOverlay != null)
                    {
                        cooldownOverlay.fillAmount = gcdProgress;
                        cooldownOverlay.gameObject.SetActive(true);
                    }
                    
                    if (cooldownText != null)
                    {
                        cooldownText.gameObject.SetActive(false); // No text for GCD
                    }
                }
                else
                {
                    // Skill is fully ready
                    if (cooldownOverlay != null)
                        cooldownOverlay.gameObject.SetActive(false);
                    if (cooldownText != null)
                        cooldownText.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // No skill, hide cooldown UI
            if (cooldownOverlay != null)
                cooldownOverlay.gameObject.SetActive(false);
            if (cooldownText != null)
                cooldownText.gameObject.SetActive(false);
        }
    }
    
    void OnActionBarChanged(ActionBarChangedEvent e)
    {
        if (e.slotIndex == slotIndex)
        {
            currentSkill = e.skill;
            RefreshDisplay();
        }
    }
    
    void OnCooldownUpdated(SkillCooldownUpdatedEvent e)
    {
        // Cooldown is handled in Update for smooth animation
    }
    
    void RefreshDisplay()
    {
        // Re-fetch skill from service
        if (skillService == null)
        {
            skillService = Services.Get<ISkillService>();
        }
        
        if (skillService != null)
        {
            currentSkill = skillService.GetActionBarSkill(slotIndex);
        }
        
        if (currentSkill != null && currentSkill.icon != null)
        {
            // Show skill icon
            if (skillIcon != null)
            {
                skillIcon.sprite = currentSkill.icon;
                skillIcon.color = Color.white;
            }
            
            // Show mana cost
            if (manaCostText != null)
            {
                if (currentSkill.manaCost > 0)
                {
                    manaCostText.text = currentSkill.manaCost.ToString("F0");
                    manaCostText.gameObject.SetActive(true);
                }
                else
                {
                    manaCostText.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // Empty slot
            if (skillIcon != null)
            {
                if (emptySlotSprite != null)
                {
                    skillIcon.sprite = emptySlotSprite;
                }
                else
                {
                    skillIcon.sprite = null;
                }
                skillIcon.color = emptySlotColor;
            }
            
            if (manaCostText != null)
            {
                manaCostText.gameObject.SetActive(false);
            }
        }
    }
    
    void OnSlotClicked()
    {
        if (currentSkill != null)
        {
            // Right-click behavior would clear slot
            // For now, clicking shows tooltip or does nothing special
        }
    }
    
    /// <summary>
    /// Set skill directly (for programmatic assignment)
    /// </summary>
    public void SetSkill(SkillData skill)
    {
        if (skillService != null)
        {
            skillService.SetActionBarSkill(slotIndex, skill);
        }
        currentSkill = skill;
        RefreshDisplay();
    }
    
    /// <summary>
    /// Clear this slot
    /// </summary>
    public void ClearSlot()
    {
        if (skillService != null)
        {
            skillService.ClearActionBarSlot(slotIndex);
        }
        currentSkill = null;
        RefreshDisplay();
    }
    
    // ==================== Drag & Drop ====================
    
    public void OnDrop(PointerEventData eventData)
    {
        // Check if dragged object has skill data component
        SkillDragData dragData = eventData.pointerDrag?.GetComponent<SkillDragData>();
        if (dragData != null && dragData.skill != null)
        {
            SetSkill(dragData.skill);
            return;
        }
        
        // Check if dragging from spellbook
        SpellbookSkillUI spellbookSkill = eventData.pointerDrag?.GetComponent<SpellbookSkillUI>();
        if (spellbookSkill != null && spellbookSkill.GetSkill() != null)
        {
            SetSkill(spellbookSkill.GetSkill());
            return;
        }
        
        // Check for dragging from another skill slot
        SkillSlotUI otherSlot = eventData.pointerDrag?.GetComponent<SkillSlotUI>();
        if (otherSlot != null && otherSlot != this && otherSlot.currentSkill != null)
        {
            // Swap skills between slots
            SkillData otherSkill = otherSlot.currentSkill;
            SkillData thisSkill = currentSkill;
            
            SetSkill(otherSkill);
            otherSlot.SetSkill(thisSkill);
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSkill == null || currentSkill.icon == null)
            return;
        
        isDragging = true;
        
        // Create drag icon
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(parentCanvas.transform, false);
        
        dragIconRect = dragIcon.AddComponent<RectTransform>();
        dragIconRect.sizeDelta = new Vector2(50f, 50f);
        
        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = currentSkill.icon;
        dragImage.raycastTarget = false;
        
        CanvasGroup canvasGroup = dragIcon.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;
        
        // Add SkillDragData for drop targets
        SkillDragData dragData = dragIcon.AddComponent<SkillDragData>();
        dragData.skill = currentSkill;
        dragData.sourceSlotIndex = slotIndex;
        
        // Position at cursor
        UpdateDragIconPosition(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragIcon == null)
            return;
        
        UpdateDragIconPosition(eventData);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
        
        // Check what we dropped on
        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
        
        // If dropped on another SkillSlotUI, the swap is handled by OnDrop
        if (dropTarget != null && dropTarget.GetComponent<SkillSlotUI>() != null)
        {
            return;
        }
        
        // If dropped on a parent of SkillSlotUI (like the slot's background), don't clear
        if (dropTarget != null && dropTarget.GetComponentInParent<SkillSlotUI>() != null)
        {
            return;
        }
        
        // Dropped outside any action bar slot - clear this slot
        ClearSlot();
    }
    
    void UpdateDragIconPosition(PointerEventData eventData)
    {
        if (dragIconRect == null || parentCanvas == null)
            return;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        
        dragIconRect.localPosition = localPoint;
    }
    
    // ==================== Tooltip ====================
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentSkill != null)
        {
            // Get player attack power for damage calculation in tooltip
            float attackPower = 0f;
            var characterService = Services.Get<ICharacterService>();
            if (characterService != null)
            {
                CharacterData charData = characterService.GetCharacterData();
                if (charData != null)
                {
                    attackPower = charData.GetAttackPower();
                }
            }
            
            EventBus.Publish(new TooltipShowEvent 
            { 
                title = currentSkill.skillName, 
                description = currentSkill.GetFullDescription(attackPower) 
            });
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        EventBus.Publish(new TooltipHideEvent());
    }
}

/// <summary>
/// Component attached to drag icon to carry skill data
/// </summary>
public class SkillDragData : MonoBehaviour
{
    public SkillData skill;
    public int sourceSlotIndex = -1;
}

