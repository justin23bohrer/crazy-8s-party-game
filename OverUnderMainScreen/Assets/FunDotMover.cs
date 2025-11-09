using UnityEngine;
using System.Collections;

/// <summary>
/// Makes a small dot move around randomly inside a larger dot container
/// Provides fun background animation with smooth movement
/// </summary>
public class FunDotMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the dot moves around")]
    public float moveSpeed = 100f;
    
    [Tooltip("How often to pick a new random target (in seconds)")]
    public float changeDirectionInterval = 1f;
    
    [Tooltip("Radius of the big dot container (how far the small dot can move)")]
    public float containerRadius = 50f;
    
    [Tooltip("Minimum distance from center before picking new target")]
    public float minDistanceFromCenter = 10f;
    
    [Header("Smooth Movement")]
    [Tooltip("How smooth the movement is (higher = more smooth)")]
    public float smoothness = 5f;
    
    private Vector3 targetPosition;
    private Vector3 initialPosition;
    private Coroutine movementCoroutine;
    
    void Start()
    {
        // Store the initial position as our center point
        initialPosition = transform.localPosition;
        
        // Start the small dot at the center of the big dot
        transform.localPosition = Vector3.zero; // Center it at (0,0,0) relative to parent
        
        // Wait a frame then start movement to ensure everything is initialized
        StartCoroutine(DelayedStart());
        
        // Debug.Log($"🚀 FunDotMover started on {gameObject.name} with container radius: {containerRadius}");
        // Debug.Log($"🚀 Initial position set to center: {transform.localPosition}");
    }
    
    System.Collections.IEnumerator DelayedStart()
    {
        yield return new WaitForEndOfFrame();
        
        // Debug.Log($"🚀 Starting delayed movement for {gameObject.name}");
        StartRandomMovement();
    }
    
    void StartRandomMovement()
    {
        // Don't start coroutines on inactive GameObjects
        if (!gameObject.activeInHierarchy)
        {
            // Debug.Log($"⚠️ Cannot start movement on inactive GameObject: {gameObject.name}");
            return;
        }
        
        // Debug.Log($"🎯 StartRandomMovement called for {gameObject.name}");
        
        // Stop any existing movement
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            // Debug.Log($"🛑 Stopped existing movement coroutine for {gameObject.name}");
        }
        
        // Pick an initial random target
        PickNewRandomTarget();
        
        // Start the movement coroutine
        movementCoroutine = StartCoroutine(MoveRandomly());
        // Debug.Log($"▶️ Started movement coroutine for {gameObject.name}");
    }
    
    void PickNewRandomTarget()
    {
        // Generate a random point within the container circle
        // Use a smaller radius to ensure the small dot stays completely inside
        float effectiveRadius = containerRadius * 0.8f; // 80% of container to stay well inside
        Vector2 randomPoint = Random.insideUnitCircle * effectiveRadius;
        
        // Convert to local position relative to the container CENTER (not initial position)
        targetPosition = new Vector3(randomPoint.x, randomPoint.y, 0f);
        
        // Debug.Log($"🎯 {gameObject.name}: New target {targetPosition} (radius: {effectiveRadius})");
        // Debug.Log($"🎯 Current position: {transform.localPosition}");
        // Debug.Log($"🎯 Distance to target: {Vector3.Distance(transform.localPosition, targetPosition)}");
    }
    
    IEnumerator MoveRandomly()
    {
        // Debug.Log($"🔄 MoveRandomly coroutine started for {gameObject.name}");
        
        while (true)
        {
            // Debug.Log($"🔄 Moving towards target: {targetPosition} from {transform.localPosition}");
            
            // Move towards the current target
            while (Vector3.Distance(transform.localPosition, targetPosition) > 5f)
            {
                // Smooth movement towards target
                Vector3 oldPosition = transform.localPosition;
                transform.localPosition = Vector3.Lerp(
                    transform.localPosition, 
                    targetPosition, 
                    smoothness * Time.deltaTime
                );
                
                // Debug every few frames to avoid spam
                if (Time.frameCount % 60 == 0) // Every 60 frames (about 1 second at 60fps)
                {
                    // Debug.Log($"🔄 {gameObject.name} moving: {oldPosition} → {transform.localPosition} (target: {targetPosition})");
                }
                
                yield return null; // Wait for next frame
            }
            
            // Debug.Log($"✅ {gameObject.name} reached target {targetPosition}");
            
            // We've reached the target, wait a bit then pick a new one
            yield return new WaitForSeconds(changeDirectionInterval);
            PickNewRandomTarget();
        }
    }
    
    /// <summary>
    /// Update the container radius (useful when connecting to card colors later)
    /// </summary>
    public void SetContainerRadius(float newRadius)
    {
        containerRadius = newRadius;
        // Debug.Log($"Container radius updated to: {containerRadius}");
    }
    
    /// <summary>
    /// Update the movement speed (useful for different animation intensities)
    /// </summary>
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        smoothness = newSpeed / 25f; // Adjust smoothness proportionally
        // Debug.Log($"Move speed updated to: {moveSpeed}");
    }
    
    /// <summary>
    /// Pause the movement animation
    /// </summary>
    public void PauseMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
    }
    
    /// <summary>
    /// Resume the movement animation
    /// </summary>
    public void ResumeMovement()
    {
        // Only resume if the GameObject is active
        if (gameObject.activeInHierarchy && movementCoroutine == null)
        {
            StartRandomMovement();
        }
    }
    
    /// <summary>
    /// Test method to force immediate movement - call from Inspector
    /// </summary>
    [ContextMenu("Force Move Test")]
    public void ForceMovementTest()
    {
        // Debug.Log($"🧪 FORCE MOVEMENT TEST for {gameObject.name}");
        // Debug.Log($"🧪 Current local position: {transform.localPosition}");
        // Debug.Log($"🧪 Container radius: {containerRadius}");
        
        // Force pick a new target
        PickNewRandomTarget();
        
        // Test immediate movement to a safe position inside the circle
        Vector3 testTarget = new Vector3(containerRadius * 0.3f, containerRadius * 0.3f, 0f);
        transform.localPosition = testTarget;
        // Debug.Log($"🧪 Moved to test position: {testTarget}");
        
        // Wait a moment then reset to center and restart movement
        StartCoroutine(ResetAfterTest());
    }
    
    System.Collections.IEnumerator ResetAfterTest()
    {
        yield return new WaitForSeconds(1f);
        transform.localPosition = Vector3.zero; // Back to center
        
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        StartRandomMovement();
    }
    
    /// <summary>
    /// Show current status
    /// </summary>
    [ContextMenu("Show Status")]
    public void ShowStatus()
    {
        // Debug.Log($"📊 STATUS for {gameObject.name}:");
        // Debug.Log($"📊 Current position: {transform.localPosition}");
        // Debug.Log($"📊 Target position: {targetPosition}");
        // Debug.Log($"📊 Initial position: {initialPosition}");
        // Debug.Log($"📊 Movement active: {movementCoroutine != null}");
        // Debug.Log($"📊 Distance to target: {Vector3.Distance(transform.localPosition, targetPosition)}");
        // Debug.Log($"📊 Container radius: {containerRadius}");
        // Debug.Log($"📊 GameObject active: {gameObject.activeInHierarchy}");
    }
    
    /// <summary>
    /// Force instant movement test - makes the dot jump around immediately
    /// </summary>
    [ContextMenu("Force Instant Movement")]
    public void ForceInstantMovement()
    {
        // Debug.Log($"⚡ FORCE INSTANT MOVEMENT for {gameObject.name}");
        
        // Jump to a few different positions to test visibility
        Vector3[] testPositions = {
            new Vector3(30f, 0f, 0f),
            new Vector3(0f, 30f, 0f),
            new Vector3(-30f, 0f, 0f),
            new Vector3(0f, -30f, 0f),
            Vector3.zero
        };
        
        StartCoroutine(TestMovementSequence(testPositions));
    }
    
    System.Collections.IEnumerator TestMovementSequence(Vector3[] positions)
    {
        foreach (Vector3 pos in positions)
        {
            transform.localPosition = pos;
            // Debug.Log($"⚡ Jumped to: {pos}");
            yield return new WaitForSeconds(0.5f);
        }
        
        // Debug.Log($"⚡ Test complete, restarting normal movement");
        StartRandomMovement();
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw the container radius in the editor for visualization
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? initialPosition : transform.localPosition;
        Vector3 worldCenter = transform.parent != null ? transform.parent.position + center : center;
        
        // Draw a wire sphere to show the movement boundary
        Gizmos.DrawWireSphere(worldCenter, containerRadius);
        
        // Draw the target position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.parent.position + targetPosition, 5f);
        }
    }
}

