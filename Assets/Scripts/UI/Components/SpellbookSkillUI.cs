using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI component for a single skill entry in the spellbook.
/// Supports dragging to action bar.
/// </summary>
public class SpellbookSkillUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image skillIcon;
    public TextMeshProUGUI skillNameText;
    public Image backgroundImage;
    
    [Header("Drag Settings")]
    public Canvas parentCanvas;
    
    [Header("Colors")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    public Color hoverColor = new Color(0.3f, 0.3f, 0.4f, 0.9f);
    
    // Current skill
    private SkillData skill;
    
    // Drag state
    private bool isDragging = false;
    private GameObject dragIcon;
    private RectTransform dragIconRect;
    
    /// <summary>
    /// Setup this UI element with skill data
    /// </summary>
    public void Setup(SkillData skillData, Canvas canvas)
    {
        skill = skillData;
        parentCanvas = canvas;
        
        if (skill == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // Set icon
        if (skillIcon != null && skill.icon != null)
        {
            skillIcon.sprite = skill.icon;
            skillIcon.color = Color.white;
        }
        
        // Set name
        if (skillNameText != null)
        {
            skillNameText.text = skill.skillName;
        }
        
        // Set background color
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
        
        gameObject.SetActive(true);
    }
    
    public SkillData GetSkill()
    {
        return skill;
    }
    
    // ==================== Drag & Drop ====================
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (skill == null || skill.icon == null)
            return;
        
        isDragging = true;
        
        // Create drag icon
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(parentCanvas.transform, false);
        
        dragIconRect = dragIcon.AddComponent<RectTransform>();
        dragIconRect.sizeDelta = new Vector2(50f, 50f);
        
        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = skill.icon;
        dragImage.raycastTarget = false;
        
        CanvasGroup canvasGroup = dragIcon.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;
        
        // Add SkillDragData for drop targets
        SkillDragData dragData = dragIcon.AddComponent<SkillDragData>();
        dragData.skill = skill;
        dragData.sourceSlotIndex = -1; // From spellbook
        
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
    
    // ==================== Hover Effects ====================
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Highlight
        if (backgroundImage != null)
        {
            backgroundImage.color = hoverColor;
        }
        
        // Show tooltip
        if (skill != null)
        {
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
                title = skill.skillName, 
                description = skill.GetFullDescription(attackPower) 
            });
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // Remove highlight
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
        
        // Hide tooltip
        EventBus.Publish(new TooltipHideEvent());
    }
}

