using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Handles the spectacular full-screen spiral animation when an 8 card color is chosen.
/// Creates a dramatic spinning spiral that covers the whole screen, then reveals the transformed 8 card.
/// </summary>
public class SpiralAnimationController : MonoBehaviour
{
    [Header("Animation Components")]
    public Image fullScreenSpiralImage; // The spiral image that will cover the whole screen
    public CanvasGroup spiralCanvasGroup; // For fade in/out effects
    
    [Header("Animation Settings")]
    public float spiralSpinDuration = 2f; // How long the spiral spins before revealing card
    public float spiralGrowDuration = 0.8f; // How long it takes to grow to full screen
    public float spiralFadeOutDuration = 0.5f; // How long it takes to fade out
    public float spiralSpinSpeed = 360f; // Degrees per second spinning speed
    
    [Header("Spiral Visuals")]
    public Sprite spiralSprite; // The spiral image to use
    public Color spiralBaseColor = Color.white; // Base color for the spiral
    
    [Header("Size Animation")]
    public float startScale = 0.1f; // Starting size of spiral (very small)
    public float endScale = 5f; // Ending size of spiral (covers whole screen)
    
    [Header("Background Color Animation")]
    public bool animateBackgroundColor = true; // Enable background color animation with spiral
    public float backgroundColorTransitionDuration = 3.3f; // How long background color transition takes (spans entire animation)
    
    // Private variables
    private bool isAnimating = false;
    private CardController targetCardController;
    private string chosenColor;
    private GameManager gameManager; // Reference to GameManager for background color control
    
    void Start()
    {
        // // Debug.Log("=== SpiralAnimationController Start() (APPLE TV MODE) ===");
        
        // APPLE TV: Ensure this component works independently of any input
        // No mouse/keyboard dependencies for animation system
        // // Debug.Log("📺 APPLE TV: Initializing phone-controlled spiral animation system");
        
        // Find GameManager for background color animation
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                // // Debug.Log("✅ Found GameManager for background color animation");
            }
            else
            {
                // // Debug.LogWarning("⚠️ GameManager not found - background color animation disabled");
                animateBackgroundColor = false;
            }
        }
        
        // Auto-setup if components not assigned
        if (fullScreenSpiralImage == null)
        {
            // // Debug.Log("Creating full screen spiral image component...");
            SetupFullScreenSpiralImage();
            
            // Add verification after setup
            if (fullScreenSpiralImage == null)
            {
                // // Debug.LogError("❌ FAILED to create fullScreenSpiralImage!");
            }
            else
            {
                // // Debug.Log("✅ Successfully created fullScreenSpiralImage: " + fullScreenSpiralImage.name);
            }
        }
        
        // Auto-assign spiral sprite if not set
        if (spiralSprite == null)
        {
            // // Debug.Log("Auto-finding spiral sprite...");
            AutoFindSpiralSprite();
            
            // Add verification
            if (spiralSprite == null)
            {
                // // Debug.LogError("❌ FAILED to find spiral sprite!");
            }
            else
            {
                // // Debug.Log("✅ Successfully found spiral sprite: " + spiralSprite.name);
            }
        }
        
        // Make sure spiral starts hidden
        HideSpiralInstantly();
        
        // // Debug.Log("=== SpiralAnimationController Setup Complete ===");
        
        // Add final verification
        if (fullScreenSpiralImage != null && spiralCanvasGroup != null)
        {
            // // Debug.Log("✅ All components ready for animation!");
        }
        else
        {
            // // Debug.LogError("❌ Missing critical components!");
            // // Debug.LogError($"fullScreenSpiralImage: {fullScreenSpiralImage}");
            // // Debug.LogError($"spiralCanvasGroup: {spiralCanvasGroup}");
        }
    }
    
    /// <summary>
    /// Auto-find the SpiralColor sprite from the project
    /// </summary>
    private void AutoFindSpiralSprite()
    {
        // // Debug.Log("🔍 Starting AutoFindSpiralSprite...");
        
        // Method 1: Try Resources folder
        // // Debug.Log("Trying Resources.Load<Sprite>(\"SpiralColor\")...");
        spiralSprite = Resources.Load<Sprite>("SpiralColor");
        if (spiralSprite != null)
        {
            // // Debug.Log("✅ Found SpiralColor in Resources folder");
            return;
        }
        // // Debug.Log("❌ SpiralColor not found in Resources folder");
        
        // Method 2: Look for it on existing images in scene
        // // Debug.Log("Searching existing images in scene...");
        Image[] allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);
        // // Debug.Log($"Found {allImages.Length} images in scene");
        
        foreach (Image img in allImages)
        {
            if (img.sprite != null && img.sprite.name.Contains("SpiralColor"))
            {
                spiralSprite = img.sprite;
                // // Debug.Log("✅ Found SpiralColor sprite on: " + img.name);
                return;
            }
        }
        // // Debug.Log("❌ SpiralColor sprite not found on any existing images");
        
        // Method 3: Check CardController for eightCardSprite
        // // Debug.Log("Checking CardController for eightCardSprite...");
        CardController cardController = FindFirstObjectByType<CardController>();
        if (cardController != null && cardController.eightCardSprite != null)
        {
            spiralSprite = cardController.eightCardSprite;
            // // Debug.Log("✅ Using CardController's eightCardSprite");
            return;
        }
        // // Debug.Log("❌ No suitable sprite found in CardController");
        
        // // Debug.LogWarning("❌ Could not auto-find spiral sprite. Animation will use solid color.");
        // // Debug.LogWarning("To fix: Assign SpiralColor.png manually in Inspector or put it in Resources folder.");
    }
    
    /// <summary>
    /// Triggers the spectacular full-screen spiral animation for an 8 card color choice
    /// </summary>
    /// <param name="cardController">The card that will be transformed</param>
    /// <param name="newColor">The color the card will become</param>
    public void TriggerSpiralAnimation(CardController cardController, string newColor)
    {
        // Force start to run first
        Start();
        
        if (isAnimating)
        {
            // // Debug.LogWarning("Spiral animation already in progress!");
            return;
        }
        
        // // Debug.Log($"=== TRIGGERING SPECTACULAR SPIRAL ANIMATION ===");
        // // Debug.Log($"Card: {cardController.name}, New Color: {newColor}");
        
        // Force setup if components are missing
        if (fullScreenSpiralImage == null || spiralCanvasGroup == null)
        {
            // // Debug.Log("⚠️ Components missing - forcing setup...");
            
            if (fullScreenSpiralImage == null)
            {
                // // Debug.Log("Setting up fullScreenSpiralImage...");
                try
                {
                    SetupFullScreenSpiralImage();
                    // // Debug.Log("✅ SetupFullScreenSpiralImage completed");
                }
                catch (System.Exception)
                {
                    // // Debug.LogError($"❌ SetupFullScreenSpiralImage failed: {e.Message}");
                    // // Debug.LogError($"Stack trace: {e.StackTrace}");
                    return;
                }
            }
            
            if (spiralSprite == null)
            {
                // // Debug.Log("Setting up spiral sprite...");
                try
                {
                    AutoFindSpiralSprite();
                    // // Debug.Log("✅ AutoFindSpiralSprite completed");
                }
                catch (System.Exception)
                {
                    // // Debug.LogError($"❌ AutoFindSpiralSprite failed: {e.Message}");
                    return;
                }
            }
        }
        
        // Verify setup worked
        if (fullScreenSpiralImage == null)
        {
            // // Debug.LogError("❌ CRITICAL: Cannot animate - fullScreenSpiralImage is still null after setup!");
            return;
        }
        
        if (spiralCanvasGroup == null)
        {
            // // Debug.LogError("❌ CRITICAL: Cannot animate - spiralCanvasGroup is still null after setup!");
            return;
        }
        
        // // Debug.Log("✅ Components verified - starting animation...");
        
        targetCardController = cardController;
        chosenColor = newColor;
        
        // Create target card data for background animation
        CardData targetCard = new CardData(newColor, 8); // Assuming it's an 8 card for Crazy 8s
        
        StartCoroutine(PlaySpiralAnimationSequence(targetCard));
    }
    
    /// <summary>
    /// The main animation sequence: grow spiral -> spin -> fade out -> reveal transformed card
    /// </summary>
    private IEnumerator PlaySpiralAnimationSequence(CardData transformationCard)
    {
        isAnimating = true;
        
        // Background color animation - start immediately with spiral
        if (animateBackgroundColor && gameManager != null)
        {
            // Debug.Log("🎨 Starting background color animation");
            StartCoroutine(AnimateBackgroundColor(transformationCard));
        }
        
        // PHASE 1: Show and grow the spiral from card position to full screen
        // Debug.Log("Phase 1: Growing spiral to full screen...");
        yield return StartCoroutine(GrowSpiralToFullScreen());
        
        // PHASE 2: Spin the spiral dramatically
        // Debug.Log("Phase 2: Spinning spiral dramatically...");
        yield return StartCoroutine(SpinSpiralDramatically());
        
        // PHASE 3: Transform the card (behind the spiral)
        // Debug.Log("Phase 3: Transforming card behind spiral...");
        TransformCardToChosenColor();
        
        // PHASE 4: Fade out spiral to reveal transformed card
        // Debug.Log("Phase 4: Revealing transformed card...");
        yield return StartCoroutine(FadeOutSpiralRevealCard());
        
        // Debug.Log("=== SPIRAL ANIMATION COMPLETE! ===");
        isAnimating = false;
        
        // Notify GameManager that animation is complete (if method exists)
        if (gameManager != null)
        {
            try
            {
                var method = gameManager.GetType().GetMethod("OnSpiralAnimationComplete");
                if (method != null)
                {
                    method.Invoke(gameManager, null);
                    // Debug.Log("✅ Notified GameManager of animation completion");
                }
                else
                {
                    // Debug.Log("⚠️ OnSpiralAnimationComplete method not found in GameManager");
                }
            }
            catch (System.Exception)
            {
                // Debug.LogWarning($"⚠️ Could not notify GameManager: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Phase 1: Grow spiral from small size to full screen coverage
    /// </summary>
    private IEnumerator GrowSpiralToFullScreen()
    {
        // Add safety checks
        if (fullScreenSpiralImage == null)
        {
            // Debug.LogError("❌ Cannot grow spiral - fullScreenSpiralImage is null!");
            yield break;
        }
        
        if (spiralCanvasGroup == null)
        {
            // Debug.LogError("❌ Cannot grow spiral - spiralCanvasGroup is null!");
            yield break;
        }
        
        // Debug.Log($"✅ Starting spiral grow with image: {fullScreenSpiralImage.name}");
        // Debug.Log($"✅ Spiral sprite: {(fullScreenSpiralImage.sprite != null ? fullScreenSpiralImage.sprite.name : "NULL")}");
        
        // Ensure spiral is positioned at screen center
        RectTransform rectTransform = fullScreenSpiralImage.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero; // Centered
        
        // Start small and invisible
        fullScreenSpiralImage.transform.localScale = Vector3.one * startScale;
        spiralCanvasGroup.alpha = 0f;
        
        // Show the spiral
        fullScreenSpiralImage.gameObject.SetActive(true);
        
        float elapsedTime = 0f;
        while (elapsedTime < spiralGrowDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / spiralGrowDuration;
            
            // Smooth growth curve
            float easeProgress = 1f - Mathf.Pow(1f - progress, 3f); // Ease out cubic
            
            // Scale up the spiral
            float currentScale = Mathf.Lerp(startScale, endScale, easeProgress);
            fullScreenSpiralImage.transform.localScale = Vector3.one * currentScale;
            
            // Fade in the spiral
            spiralCanvasGroup.alpha = Mathf.Lerp(0f, 1f, easeProgress);
            
            yield return null;
        }
        
        // Ensure final state
        fullScreenSpiralImage.transform.localScale = Vector3.one * endScale;
        spiralCanvasGroup.alpha = 1f;
        
        // Debug.Log("Spiral grown to full screen!");
    }
    
    /// <summary>
    /// Phase 2: Spin the spiral dramatically while it covers the screen
    /// </summary>
    private IEnumerator SpinSpiralDramatically()
    {
        float elapsedTime = 0f;
        float startRotation = fullScreenSpiralImage.transform.eulerAngles.z;
        
        while (elapsedTime < spiralSpinDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Continuous spinning
            float rotationAmount = spiralSpinSpeed * Time.deltaTime;
            fullScreenSpiralImage.transform.Rotate(0, 0, rotationAmount);
            
            // Optional: Add some color pulsing to make it more dramatic
            float pulseIntensity = Mathf.Sin(elapsedTime * 8f) * 0.2f + 0.8f; // Pulse between 0.6 and 1.0
            Color currentColor = spiralBaseColor;
            currentColor.a = pulseIntensity;
            fullScreenSpiralImage.color = currentColor;
            
            yield return null;
        }
        
        // Debug.Log("Spiral spinning complete!");
    }
    
    /// <summary>
    /// Phase 3: Transform the card to the chosen color (happens behind the spiral)
    /// </summary>
    private void TransformCardToChosenColor()
    {
        if (targetCardController != null && !string.IsNullOrEmpty(chosenColor))
        {
            // Debug.Log($"Transforming card to {chosenColor} behind the spiral...");
            targetCardController.SetCard(chosenColor, 8, true); // Force the 8 card to the chosen color
            // Debug.Log("Card transformation complete!");
        }
        else
        {
            // Debug.LogError("Cannot transform card - missing card controller or color!");
        }
    }
    
    /// <summary>
    /// Phase 4: Fade out the spiral to dramatically reveal the transformed card
    /// </summary>
    private IEnumerator FadeOutSpiralRevealCard()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < spiralFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / spiralFadeOutDuration;
            
            // Fade out the spiral
            spiralCanvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            
            // Slow down the spinning as it fades
            float spinSlowdown = Mathf.Lerp(1f, 0.1f, progress);
            float rotationAmount = spiralSpinSpeed * spinSlowdown * Time.deltaTime;
            fullScreenSpiralImage.transform.Rotate(0, 0, rotationAmount);
            
            yield return null;
        }
        
        // Hide the spiral completely
        HideSpiralInstantly();
        
        // Debug.Log("Spiral faded out - transformed card revealed!");
    }
    
    /// <summary>
    /// Instantly hide the spiral (used for initialization and cleanup)
    /// </summary>
    public void HideSpiralInstantly()
    {
        if (fullScreenSpiralImage != null)
        {
            fullScreenSpiralImage.gameObject.SetActive(false);
        }
        
        if (spiralCanvasGroup != null)
        {
            spiralCanvasGroup.alpha = 0f;
        }
        
        isAnimating = false;
    }
    
    /// <summary>
    /// Auto-setup the full screen spiral image if not manually assigned
    /// </summary>
    private void SetupFullScreenSpiralImage()
    {
        // Debug.Log("🔧 Starting SetupFullScreenSpiralImage...");
        
        try
        {
            // Create a Canvas for the spiral overlay
            // Debug.Log("Creating SpiralAnimationCanvas...");
            GameObject spiralCanvas = new GameObject("SpiralAnimationCanvas");
            Canvas canvas = spiralCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // High sorting order to appear on top
            // Debug.Log("✅ Canvas created");
            
            CanvasScaler scaler = spiralCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            // Debug.Log("✅ CanvasScaler added");
            
            spiralCanvas.AddComponent<GraphicRaycaster>();
            // Debug.Log("✅ GraphicRaycaster added");
            
            // Create the spiral image
            // Debug.Log("Creating FullScreenSpiral GameObject...");
            GameObject spiralObject = new GameObject("FullScreenSpiral");
            spiralObject.transform.SetParent(spiralCanvas.transform, false);
            
            fullScreenSpiralImage = spiralObject.AddComponent<Image>();
            // Debug.Log("✅ Image component added");
            
            // Setup spiral image properties
            if (spiralSprite != null)
            {
                fullScreenSpiralImage.sprite = spiralSprite;
                // Debug.Log("Using assigned spiral sprite: " + spiralSprite.name);
            }
            else
            {
                // Debug.Log("No spiral sprite assigned - will use solid color");
            }
            
            fullScreenSpiralImage.color = spiralBaseColor;
            // Debug.Log("✅ Image color set");
            
            // Make it fill the screen but centered
            RectTransform rectTransform = spiralObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(400, 400); // Base size for spiral
            // Debug.Log("✅ RectTransform configured");
            
            // Add CanvasGroup for fade effects
            spiralCanvasGroup = spiralObject.AddComponent<CanvasGroup>();
            spiralCanvasGroup.alpha = 0f;
            // Debug.Log("✅ CanvasGroup added");
            
            // Debug.Log("🎉 Full screen spiral image setup complete!");
        }
        catch (System.Exception)
        {
            // Debug.LogError($"❌ Exception in SetupFullScreenSpiralImage: {e.Message}");
            // Debug.LogError($"Stack trace: {e.StackTrace}");
            throw;
        }
    }
    
    /// <summary>
    /// Test method to trigger animation manually (for debugging)
    /// </summary>
    [ContextMenu("Test Spiral Animation")]
    public void TestSpiralAnimation()
    {
        // Find a card controller to test with
        CardController testCard = FindFirstObjectByType<CardController>();
        if (testCard != null)
        {
            // Debug.Log("Testing spiral animation with yellow color...");
            TriggerSpiralAnimation(testCard, "yellow");
        }
        else
        {
            // Debug.LogError("No CardController found for testing!");
        }
    }
    
    /// <summary>
    /// Public method to check if animation is currently playing
    /// </summary>
    public bool IsAnimating()
    {
        return isAnimating;
    }
    
    /// <summary>
    /// Animates background color to match the transformed card
    /// </summary>
    private IEnumerator AnimateBackgroundColor(CardData targetCard)
    {
        if (gameManager == null || gameManager.colorChangerBackground == null)
        {
            // Debug.LogWarning("GameManager or background image not available for background color animation");
            yield break;
        }
        
        // Debug.Log($"🎨 Animating background to {targetCard.color} over {backgroundColorTransitionDuration} seconds");
        
        // Start color change immediately with the animation for smooth gradual transition
        // No delay - begin changing color right as spiral animation starts
        yield return new WaitForSeconds(0f);
        
        // Use GameManager's method to ensure consistent color blending (same as normal cards)
        Color startColor = gameManager.colorChangerBackground.color;
        
        // Get target color using the same method as normal cards
        var changeBackgroundMethod = gameManager.GetType().GetMethod("ChangeBackgroundToCardColor", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (changeBackgroundMethod != null)
        {
            // Animate the background color transition by gradually calling ChangeBackgroundToCardColor
            float elapsedTime = 0f;
            while (elapsedTime < backgroundColorTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / backgroundColorTransitionDuration;
                
                // At the end of animation, apply the final color using the same method as normal cards
                if (progress >= 1f)
                {
                    changeBackgroundMethod.Invoke(gameManager, new object[] { targetCard.color });
                    break;
                }
                
                yield return null;
            }
            
            // Ensure final color is set using the exact same method as normal cards
            changeBackgroundMethod.Invoke(gameManager, new object[] { targetCard.color });
        }
        else
        {
            // Debug.LogWarning("Could not find ChangeBackgroundToCardColor method - using fallback");
            // Fallback: use the direct method call
            Color targetColor = gameManager.GetBackgroundColor(targetCard.color);
            gameManager.colorChangerBackground.color = targetColor;
        }
        
        // Debug.Log("✅ Background color animation completed using same method as normal cards");
    }
}

