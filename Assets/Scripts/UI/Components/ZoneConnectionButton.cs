using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component for displaying a clickable zone connection button.
/// This component is used as a prefab that can be instantiated for each extra zone connection.
/// </summary>
public class ZoneConnectionButton : MonoBehaviour
{
    [Header("Connection Display")]
    public Image connectionImage; // Image showing the connection icon
    
    [Header("Button")]
    [Tooltip("Button component. If not assigned, will use or create one on this GameObject.")]
    public Button button;
    
    private ZoneConnection connectionData;
    private int connectionIndex;
    private RectTransform rectTransform;
    private IZoneService zoneService;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Get zone service
        if (!Services.TryGet<IZoneService>(out zoneService))
        {
            Debug.LogWarning("[ZoneConnectionButton] ZoneService not found!");
        }
        
        // Setup button if assigned
        if (button != null)
        {
            button.onClick.AddListener(OnConnectionClicked);
        }
        else
        {
            // Fallback: If no button assigned, use or create one on this GameObject
            Button panelButton = GetComponent<Button>();
            if (panelButton == null)
            {
                panelButton = gameObject.AddComponent<Button>();
            }
            button = panelButton;
            button.onClick.AddListener(OnConnectionClicked);
        }
    }
    
    /// <summary>
    /// Initialize this connection button with connection data, index, position, and scale
    /// </summary>
    public void Initialize(ZoneConnection connection, int index, Vector2 position, float scale)
    {
        connectionData = connection;
        connectionIndex = index;
        
        if (connection == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // Set position (use absolute pixel coordinates directly)
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
            
            // Apply scale
            rectTransform.localScale = Vector3.one * scale;
        }
        
        // Update connection image
        if (connectionImage != null)
        {
            if (connection.icon != null)
            {
                connectionImage.sprite = connection.icon;
                connectionImage.gameObject.SetActive(true);
            }
            else
            {
                // If no icon provided, hide the image
                connectionImage.gameObject.SetActive(false);
            }
        }
        
        // Show the button
        if (button != null)
        {
            button.gameObject.SetActive(true);
        }
        
        gameObject.SetActive(true);
    }
    
    void OnConnectionClicked()
    {
        if (connectionData == null || zoneService == null)
        {
            Debug.LogWarning("[ZoneConnectionButton] Cannot navigate - missing data or service");
            return;
        }
        
        // Check if we can navigate to this connection
        if (zoneService is ZoneManager zoneManager)
        {
            if (!zoneManager.CanNavigateToConnection(connectionIndex))
            {
                Debug.LogWarning($"[ZoneConnectionButton] Cannot navigate to '{connectionData.displayName}' - requirements not met");
                return;
            }
            
            // Navigate to the connection
            zoneManager.NavigateToConnection(connectionIndex);
            Debug.Log($"[ZoneConnectionButton] Navigating to '{connectionData.displayName}'");
        }
        else
        {
            Debug.LogError("[ZoneConnectionButton] ZoneService is not a ZoneManager instance");
        }
    }
}
