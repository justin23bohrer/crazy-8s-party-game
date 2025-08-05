using UnityEngine;

public class CloudMover : MonoBehaviour 
{
    [Header("Movement Settings")]
    public float speed = 50f;              // Speed of cloud movement
    public float minSpeed = 30f;           // Minimum random speed
    public float maxSpeed = 80f;           // Maximum random speed
    
    [Header("Spawn Settings")]
    public float minDelay = 1f;            // Minimum delay before respawn
    public float maxDelay = 5f;            // Maximum delay before respawn
    public float maxStartDelay = 8f;       // Maximum initial start delay - spread them out more
    
    private RectTransform rectTransform;
    private float screenWidth;
    private float cloudWidth;
    private bool isMoving = false;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Get screen width - use Screen.width for more reliable results
        screenWidth = Screen.width;
        
        // If we're in a Canvas, try to get the Canvas width instead
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            if (canvasRect != null && canvasRect.sizeDelta.x > 0)
            {
                screenWidth = canvasRect.sizeDelta.x;
            }
        }
        
        // Get cloud width
        cloudWidth = rectTransform.sizeDelta.x;
        
        // Immediately position cloud off-screen so it's never visible on start
        float offScreenX = -1400f; // Start far off screen like the example position
        rectTransform.anchoredPosition = new Vector2(offScreenX, rectTransform.anchoredPosition.y);
        
        Debug.Log($"Cloud {gameObject.name}: screenWidth={screenWidth}, cloudWidth={cloudWidth}");
        
        // Start with random delay to prevent all spawning at once - ensure minimum delay
        float startDelay = Random.Range(1f, maxStartDelay); // Start from 1 second minimum, not 0
        Invoke("StartMoving", startDelay);
    }
    
    void Update()
    {
        if (isMoving)
        {
            // Move cloud to the right
            rectTransform.anchoredPosition += Vector2.right * speed * Time.deltaTime;
            
            // Check if cloud is completely off the right side of screen
            if (rectTransform.anchoredPosition.x > screenWidth + cloudWidth)
            {
                ResetCloud();
            }
        }
    }
    
    void StartMoving()
    {
        // Set random speed
        speed = Random.Range(minSpeed, maxSpeed);
        
        // Position cloud far off the left side of screen (similar to -1370 position)
        float startX = -1300f - Random.Range(0f, 500f); // Start around -1300 to -1800 range
        float randomY = Random.Range(100f, 400f); // Higher up in the sky area
        
        rectTransform.anchoredPosition = new Vector2(startX, randomY);
        
        // Set random size for depth variation
        float randomScale = Random.Range(0.5f, 1.2f);
        transform.localScale = Vector3.one * randomScale;
        
        // Set random transparency for depth
        var image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = Random.Range(0.3f, 0.8f); // Random transparency
            image.color = color;
        }
        
        Debug.Log($"Cloud {gameObject.name} starting to move: speed={speed}, startPos=({startX}, {randomY})");
        
        isMoving = true;
    }
    
    void ResetCloud()
    {
        isMoving = false;
        
        // Wait random time before starting again
        float delay = Random.Range(minDelay, maxDelay);
        Invoke("StartMoving", delay);
    }
}