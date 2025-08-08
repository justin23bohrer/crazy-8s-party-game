using UnityEngine;
using UnityEngine.UI;

public class PolkaDotFader : MonoBehaviour
{
    [Header("Fade Settings")]
    [Range(0f, 1f)]
    public float minOpacity = 0f;           // Minimum opacity (0 = invisible)
    
    [Range(0f, 1f)]
    public float maxOpacity = 1f;         // Maximum opacity (1f = 100% - full vibrant color!)
    
    [Range(3f, 25f)]
    public float baseFadeDuration = 8f;     // Base time for each fade (will be randomized)
    
    [Range(0f, 15f)]
    public float randomDelayMax = 10f;      // Maximum random delay before starting (0-10 seconds)
    
    [Header("Dot Colors")]
    public Color[] dotColors = new Color[]
    {
        new Color(0.8f, 0.1f, 0.1f, 1f),   // Red #CC1A1A
        new Color(0.1f, 0.1f, 0.8f, 1f),   // Blue #1A1ACC
        new Color(0.1f, 0.6f, 0.1f, 1f),   // Green #1A991A
        new Color(0.8f, 0.8f, 0.1f, 1f)    // Yellow #CCCC1A
    };
    
    [Header("Components")]
    public Image dotImage;
    
    private float timer = 0f;
    private bool fadingToMax = true;        // true = fading to max, false = fading to min
    private float startDelay = 0f;
    private bool delayComplete = false;
    private Color baseColor;
    private float currentFadeDuration;      // The actual fade duration for this dot (randomized)
    
    void Start()
    {
        // Auto-find Image component if not assigned
        if (dotImage == null)
        {
            dotImage = GetComponent<Image>();
        }
        
        if (dotImage == null)
        {
            Debug.LogWarning("PolkaDotFader: No Image component found!");
            enabled = false;
            return;
        }
        
        // Pick a random color from the array
        if (dotColors.Length > 0)
        {
            baseColor = dotColors[Random.Range(0, dotColors.Length)];
        }
        else
        {
            baseColor = Color.white;
        }
        
        // ALWAYS start at minimum opacity (0) - no popping!
        Color initialColor = baseColor;
        initialColor.a = minOpacity;
        dotImage.color = initialColor;
        
        // Random delay before starting animation (0 to randomDelayMax seconds)
        startDelay = Random.Range(0f, randomDelayMax);
        
        // ALWAYS start by fading TO max (from 0 to 100%) - no random direction
        fadingToMax = true;
        
        // Randomize the fade duration for this specific dot
        currentFadeDuration = baseFadeDuration + Random.Range(-3f, 5f);
        currentFadeDuration = Mathf.Clamp(currentFadeDuration, 3f, 20f);
        
        Debug.Log($"PolkaDotFader started - Color: {baseColor}, Delay: {startDelay:F1}s, Duration: {currentFadeDuration:F1}s");
    }
    
    void Update()
    {
        // Handle initial delay
        if (!delayComplete)
        {
            startDelay -= Time.deltaTime;
            if (startDelay <= 0f)
            {
                delayComplete = true;
                timer = 0f;
            }
            return;
        }
        
        // Update timer
        timer += Time.deltaTime;
        
        // Calculate fade progress (0 to 1)
        float progress = timer / currentFadeDuration;
        
        if (progress >= 1f)
        {
            // Fade complete - switch direction and reset timer
            fadingToMax = !fadingToMax;
            timer = 0f;
            progress = 0f;
            
            // Add a small random variation to the next fade duration for organic feel
            currentFadeDuration = baseFadeDuration + Random.Range(-2f, 4f);
            currentFadeDuration = Mathf.Clamp(currentFadeDuration, 3f, 20f);
        }
        
        // Calculate current opacity based on fade direction
        float currentOpacity;
        if (fadingToMax)
        {
            // Smooth curve from min to max
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            currentOpacity = Mathf.Lerp(minOpacity, maxOpacity, smoothProgress);
        }
        else
        {
            // Smooth curve from max to min
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            currentOpacity = Mathf.Lerp(maxOpacity, minOpacity, smoothProgress);
        }
        
        // Apply the opacity to the dot
        Color newColor = baseColor;
        newColor.a = currentOpacity;
        dotImage.color = newColor;
    }
    
    // Public methods for external control
    public void SetFadeDuration(float duration)
    {
        currentFadeDuration = Mathf.Max(1f, duration);
        baseFadeDuration = duration;
    }
    
    public void SetOpacityRange(float min, float max)
    {
        minOpacity = Mathf.Clamp01(min);
        maxOpacity = Mathf.Clamp01(max);
    }
    
    public void SetRandomColor()
    {
        if (dotColors.Length > 0)
        {
            baseColor = dotColors[Random.Range(0, dotColors.Length)];
        }
    }
    
    // Editor helper - randomize the dot for testing
    [ContextMenu("Randomize Dot")]
    void RandomizeDot()
    {
        SetRandomColor();
        startDelay = Random.Range(0f, randomDelayMax);
        fadingToMax = true; // Always start by fading in
        timer = 0f;
        delayComplete = false;
        
        // Randomize fade duration
        currentFadeDuration = baseFadeDuration + Random.Range(-3f, 5f);
        currentFadeDuration = Mathf.Clamp(currentFadeDuration, 3f, 20f);
        
        Debug.Log("Dot randomized!");
    }
}
