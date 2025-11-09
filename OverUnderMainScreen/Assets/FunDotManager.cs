using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all the fun dots in the background
/// Handles animation coordination and color synchronization with game state
/// </summary>
public class FunDotManager : MonoBehaviour
{
    [Header("Dot References")]
    [Tooltip("All the BigFunDot containers in the scene")]
    public Transform[] bigFunDots;
    
    [Header("Animation Settings")]
    [Tooltip("Base movement speed for all dots")]
    public float baseMoveSpeed = 100f;
    
    [Tooltip("How often dots change direction")]
    public float baseChangeInterval = 1f;
    
    [Tooltip("Container radius for dot movement")]
    public float dotContainerRadius = 50f;
    
    [Header("Color Connection (Future)")]
    [Tooltip("Reference to GameManager for card color synchronization")]
    public GameManager gameManager;
    
    private List<FunDotMover> allDotMovers = new List<FunDotMover>();
    
    void Start()
    {
        // Debug.Log("=== FUN DOT MANAGER STARTING ===");
        
        // Find GameManager if not assigned
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                // Debug.Log("✅ Found GameManager for future color synchronization");
            }
            else
            {
                // Debug.LogWarning("⚠️ GameManager not found - color sync will not work");
            }
        }
        
        // Set up all the fun dots
        SetupAllFunDots();
        
        // Force start movement for all dots (in case they don't auto-start)
        StartCoroutine(EnsureDotsAreMoving());
        
        // Debug.Log($"=== FUN DOT MANAGER READY - {allDotMovers.Count} dots active ===");
    }
    
    /// <summary>
    /// Ensure dots start moving even if they don't auto-start
    /// </summary>
    private System.Collections.IEnumerator EnsureDotsAreMoving()
    {
        // Wait a few seconds for game to fully load
        yield return new WaitForSeconds(3f);
        
        // Debug.Log("🔍 Checking if fun dots are moving...");
        
        foreach (FunDotMover mover in allDotMovers)
        {
            if (mover != null && mover.gameObject.activeInHierarchy)
            {
                // Debug.Log($"Ensuring {mover.gameObject.name} is moving");
                mover.ResumeMovement(); // Force resume movement
            }
        }
        
        // Debug.Log($"✅ Ensured {allDotMovers.Count} active dots are moving");
    }
    
    void SetupAllFunDots()
    {
        // Always try to set up manually assigned dots first
        if (bigFunDots != null && bigFunDots.Length > 0)
        {
            // Debug.Log($"Using manually assigned BigFunDots: {bigFunDots.Length} found");
            
            // Set up each manually assigned BigFunDot
            foreach (Transform bigDot in bigFunDots)
            {
                if (bigDot != null)
                {
                    SetupSingleFunDot(bigDot);
                }
            }
        }
        else
        {
            // Debug.Log("No BigFunDots manually assigned - searching for them...");
            FindAllBigFunDots();
            
            // Set up each found BigFunDot
            foreach (Transform bigDot in bigFunDots)
            {
                if (bigDot != null)
                {
                    SetupSingleFunDot(bigDot);
                }
            }
        }
        
        // Debug.Log($"Set up {allDotMovers.Count} fun dot movers");
    }
    
    void FindAllBigFunDots()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<Transform> foundDots = new List<Transform>();
        
        // Debug.Log($"🔍 Searching for BigFunDot objects...");
        
        foreach (GameObject obj in allObjects)
        {
            // Simple check - if the name contains "BigFunDot" (case insensitive)
            if (obj.name.IndexOf("BigFunDot", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                foundDots.Add(obj.transform);
                // Debug.Log($"✅ Found: {obj.name}");
            }
        }
        
        bigFunDots = foundDots.ToArray();
        // Debug.Log($"🎯 Total BigFunDot objects found: {bigFunDots.Length}");
        
        // List them all
        for (int i = 0; i < bigFunDots.Length; i++)
        {
            // Debug.Log($"   {i + 1}. {bigFunDots[i].name}");
        }
        
        if (bigFunDots.Length == 0)
        {
            // Debug.LogError("❌ No BigFunDot objects found! Make sure your objects are named 'BigFunDot', 'BigFunDot2', etc.");
        }
    }
    
    void SetupSingleFunDot(Transform bigDot)
    {
        // Debug.Log($"Setting up BigFunDot: {bigDot.name}");
        
        // Look for SmallFunDot child
        Transform smallDot = null;
        
        // Search through children for SmallFunDot
        for (int i = 0; i < bigDot.childCount; i++)
        {
            Transform child = bigDot.GetChild(i);
            if (child.name.Contains("SmallFunDot") || child.name.Contains("smallfundot"))
            {
                smallDot = child;
                // Debug.Log($"Found SmallFunDot: {child.name}");
                break;
            }
        }
        
        if (smallDot == null)
        {
            // Debug.LogWarning($"No SmallFunDot found in {bigDot.name}");
            return;
        }
        
        // Add FunDotMover component if it doesn't exist
        FunDotMover mover = smallDot.GetComponent<FunDotMover>();
        if (mover == null)
        {
            mover = smallDot.gameObject.AddComponent<FunDotMover>();
            // Debug.Log($"Added FunDotMover to {smallDot.name}");
        }
        
        // Configure the mover
        mover.moveSpeed = baseMoveSpeed + Random.Range(-10f, 10f); // Slight variation
        mover.changeDirectionInterval = baseChangeInterval + Random.Range(-0.5f, 0.5f); // Slight variation
        mover.containerRadius = dotContainerRadius;
        
        // Add to our list for management
        allDotMovers.Add(mover);
        
        // Debug.Log($"✅ Configured FunDotMover for {smallDot.name}");
    }
    
    /// <summary>
    /// Update all dot speeds (useful for different game states)
    /// </summary>
    public void SetAllDotSpeeds(float newSpeed)
    {
        baseMoveSpeed = newSpeed;
        
        foreach (FunDotMover mover in allDotMovers)
        {
            if (mover != null)
            {
                mover.SetMoveSpeed(newSpeed + Random.Range(-10f, 10f));
            }
        }
        
        // Debug.Log($"Updated all dot speeds to base: {newSpeed}");
    }
    
    /// <summary>
    /// Pause all dot animations
    /// </summary>
    public void PauseAllDots()
    {
        foreach (FunDotMover mover in allDotMovers)
        {
            if (mover != null)
            {
                mover.PauseMovement();
            }
        }
        
        // Debug.Log("Paused all fun dot animations");
    }
    
    /// <summary>
    /// Resume all dot animations
    /// </summary>
    public void ResumeAllDots()
    {
        foreach (FunDotMover mover in allDotMovers)
        {
            if (mover != null)
            {
                mover.ResumeMovement();
            }
        }
        
        // Debug.Log("Resumed all fun dot animations");
    }
    
    /// <summary>
    /// Future: Connect dot colors to card colors
    /// This will be implemented in step 2
    /// </summary>
    public void SyncDotsWithCardColor(string cardColor)
    {
        // Debug.Log($"Future feature: Sync dots with card color: {cardColor}");
        // TODO: Implement color synchronization
    }
    
    /// <summary>
    /// Start dots moving when game actually begins (not just scene load)
    /// Call this from GameManager when game starts
    /// </summary>
    public void StartGameAnimation()
    {
        // Debug.Log("🎮 GAME STARTED - Activating fun dot animations!");
        
        // Resume all dots with a bit more energy for the game
        foreach (FunDotMover mover in allDotMovers)
        {
            if (mover != null)
            {
                mover.ResumeMovement();
                
                // Boost movement for game excitement
                mover.SetMoveSpeed(baseMoveSpeed + 50f); // Extra speed during game
                mover.SetContainerRadius(dotContainerRadius + 20f); // Bigger movement area
                
                // Debug.Log($"🎮 Activated game animation for {mover.gameObject.name}");
            }
        }
        
        // Debug.Log($"🎮 Game animations started for {allDotMovers.Count} dots");
    }
    
    /// <summary>
    /// Stop dots when game ends or goes back to lobby
    /// </summary>
    public void StopGameAnimation()
    {
        // Debug.Log("🛑 GAME ENDED - Slowing down fun dot animations");
        
        foreach (FunDotMover mover in allDotMovers)
        {
            if (mover != null)
            {
                // Reduce to calm lobby animation
                mover.SetMoveSpeed(baseMoveSpeed * 0.5f); // Slower during lobby
                mover.SetContainerRadius(dotContainerRadius * 0.7f); // Smaller movement area
                
                // Debug.Log($"🛑 Slowed animation for {mover.gameObject.name}");
            }
        }
        
        // Debug.Log("🛑 Game animations slowed for lobby mode");
    }
    
    /// <summary>
    /// Test method to verify all dots are working
    /// </summary>
    [ContextMenu("Test All Dots")]
    public void TestAllDots()
    {
        // Debug.Log("=== TESTING ALL FUN DOTS ===");
        
        for (int i = 0; i < allDotMovers.Count; i++)
        {
            FunDotMover mover = allDotMovers[i];
            if (mover != null)
            {
                // Debug.Log($"Dot {i}: {mover.gameObject.name} - Active: {mover.gameObject.activeInHierarchy}");
                
                // Force immediate test movement
                mover.ForceInstantMovement();
            }
            else
            {
                // Debug.LogWarning($"Dot {i}: NULL reference");
            }
        }
        
        // Debug.Log($"Total active dots: {allDotMovers.Count}");
    }
    
    /// <summary>
    /// Force all dots to move with bigger, more visible movement
    /// </summary>
    [ContextMenu("Force All Dots Big Movement")]
    public void ForceAllDotsBigMovement()
    {
        // Debug.Log("=== FORCING BIG MOVEMENT FOR ALL DOTS ===");
        
        foreach (FunDotMover mover in allDotMovers)
        {
            if (mover != null)
            {
                // Increase container radius for more visible movement
                mover.SetContainerRadius(100f);
                mover.SetMoveSpeed(200f);
                
                // Force restart movement
                mover.ResumeMovement();
                
                // Debug.Log($"✅ Boosted movement for {mover.gameObject.name}");
            }
        }
        
        // Debug.Log($"Boosted movement for {allDotMovers.Count} dots");
    }
    
    /// <summary>
    /// Manual setup method you can call from Inspector
    /// </summary>
    [ContextMenu("Force Setup Dots")]
    public void ForceSetupDots()
    {
        // Debug.Log("=== FORCE SETUP DOTS ===");
        
        // Clear existing
        allDotMovers.Clear();
        
        // Re-setup
        SetupAllFunDots();
        
        // Debug.Log($"Force setup complete - {allDotMovers.Count} dots configured");
    }
    
    /// <summary>
    /// List all objects in scene for debugging
    /// </summary>
    [ContextMenu("List All Scene Objects")]
    public void ListAllSceneObjects()
    {
        // Debug.Log("=== ALL SCENE OBJECTS ===");
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            // Debug.Log($"Object: {obj.name} (Active: {obj.activeInHierarchy})");
        }
        
        // Debug.Log($"Total objects: {allObjects.Length}");
    }
}

