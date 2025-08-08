using UnityEngine;
using UnityEngine.UI;

public class RoomCodeBorderCycler : MonoBehaviour
{
    [Header("Color Settings")]
    public Color[] colors = new Color[]
    {
        new Color(0.8f, 0.1f, 0.1f, 1f),   // Red #CC1A1A
        new Color(0.1f, 0.1f, 0.8f, 1f),   // Blue #1A1ACC
        new Color(0.1f, 0.6f, 0.1f, 1f),   // Green #1A991A
        new Color(0.8f, 0.8f, 0.1f, 1f)    // Yellow #CCCC1A
    };
    
    [Header("Animation Settings")]
    [Range(5f, 60f)]
    public float cycleDuration = 100f; // Time to transition between colors (MUCH slower - 20 seconds!)
    
    [Range(1f, 10f)]
    public float pauseBetweenColors = 3f; // Pause time at each color (3 seconds)
    
    [Header("Components")]
    public Image targetImage; // The border image to color
    public Outline targetOutline; // Alternative: outline component
    
    private int currentColorIndex = 0;
    private int nextColorIndex = 1;
    private float timer = 0f;
    private bool isPausing = false;
    
    void Start()
    {
        // Auto-find components if not assigned
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        
        if (targetOutline == null)
        {
            targetOutline = GetComponent<Outline>();
        }
        
        // Validate we have something to color
        if (targetImage == null && targetOutline == null)
        {
            Debug.LogWarning("RoomCodeBorderCycler: No Image or Outline component found! Please assign targetImage or targetOutline.");
            enabled = false;
            return;
        }
        
        // Set initial color
        if (colors.Length > 0)
        {
            currentColorIndex = 0;
            nextColorIndex = colors.Length > 1 ? 1 : 0;
            SetColor(colors[currentColorIndex]);
        }
        
        Debug.Log("RoomCodeBorderCycler started with " + colors.Length + " colors");
    }
    
    void Update()
    {
        if (colors.Length <= 1) return; // Need at least 2 colors to cycle
        
        timer += Time.deltaTime;
        
        if (isPausing)
        {
            // Pause at current color - stay at the solid color
            if (timer >= pauseBetweenColors)
            {
                timer = 0f;
                isPausing = false;
                // Now start transitioning to the next color
            }
            // Keep showing the current solid color during pause
            SetColor(colors[currentColorIndex]);
        }
        else
        {
            // Smooth transition between currentColorIndex and nextColorIndex
            float progress = timer / cycleDuration;
            
            if (progress >= 1f)
            {
                // Transition complete - move to next color and start pause
                currentColorIndex = nextColorIndex;
                nextColorIndex = (nextColorIndex + 1) % colors.Length;
                
                timer = 0f;
                isPausing = true;
                
                // Set to the exact current color for the pause
                SetColor(colors[currentColorIndex]);
            }
            else
            {
                // Smooth interpolation between current and next color
                // Use smooth step for even smoother easing
                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                
                Color currentColor = colors[currentColorIndex];
                Color nextColor = colors[nextColorIndex];
                Color lerpedColor = Color.Lerp(currentColor, nextColor, smoothProgress);
                
                SetColor(lerpedColor);
            }
        }
    }
    
    void SetColor(Color color)
    {
        // Apply color to Image component if available
        if (targetImage != null)
        {
            targetImage.color = color;
        }
        
        // Apply color to Outline component if available
        if (targetOutline != null)
        {
            targetOutline.effectColor = color;
        }
    }
    
    // Public methods for external control
    public void SetCycleDuration(float duration)
    {
        cycleDuration = Mathf.Max(0.1f, duration);
    }
    
    public void SetPauseDuration(float pause)
    {
        pauseBetweenColors = Mathf.Max(0f, pause);
    }
    
    public void ResetCycle()
    {
        currentColorIndex = 0;
        nextColorIndex = colors.Length > 1 ? 1 : 0;
        timer = 0f;
        isPausing = false;
        if (colors.Length > 0)
        {
            SetColor(colors[currentColorIndex]);
        }
    }
    
    public void StopCycling()
    {
        enabled = false;
    }
    
    public void StartCycling()
    {
        enabled = true;
        ResetCycle();
    }
    
    // Editor helper - call this to preview colors in inspector
    [ContextMenu("Preview Color Cycle")]
    void PreviewColorCycle()
    {
        if (colors.Length > 0)
        {
            currentColorIndex = (currentColorIndex + 1) % colors.Length;
            SetColor(colors[currentColorIndex]);
            Debug.Log("Preview: Color " + currentColorIndex + " - " + colors[currentColorIndex]);
        }
    }
}
