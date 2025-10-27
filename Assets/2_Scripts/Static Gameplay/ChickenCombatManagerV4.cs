using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChickenCombatManagerV4 : MonoBehaviour
{
    [Header("General Combat Settings")]
    public bool enableCombat = true;
    public float eggSpeed = 20f;
    public float PatternChangeCooldown;

    [Header("Difficulty Scaling")]
    [Tooltip("How much to increase egg speed every interval during enemy waves")]
    public float eggSpeedIncreaseAmount = 10f;
    [Tooltip("How much to decrease pattern cooldown every interval during enemy waves")]
    public float patternCooldownDecreaseAmount = 0.5f;
    [Tooltip("How often (in seconds) to apply difficulty increases during enemy waves")]
    public float difficultyIncreaseInterval = 30f;
    [Tooltip("Maximum egg speed that can be reached during difficulty scaling")]
    public float maxEggSpeed = 80f;
    [Tooltip("Minimum pattern cooldown that can be reached during difficulty scaling")]
    public float minPatternCooldown = 1.5f;

    [Header("Attack Configuration")]
    [Tooltip("Default attack loot table used when stage doesn't have one assigned")]
    [SerializeField] private AttackLootTableSO defaultAttackLootTable;

    [Tooltip("Current active attack loot table (can be from stage or default)")]
    public AttackLootTableSO attackLootTable;

    [Header("Fallback Attack")]
    [Tooltip("This attack will be used when no other attacks from the loot table can be executed")]
    public BaseChickenAttackSO fallbackAttack;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showAttackGizmos = false;
    public bool showRegistrationLogs = false;

    [Header("Registered Chickens (Read Only)")]
    [SerializeField] private List<ChickenCombatBehaviorV2> registeredCombatChickens = new List<ChickenCombatBehaviorV2>();

    [Header("Combat Status (Read Only)")]

    [SerializeField] private BaseChickenAttackSO currentAttackSO;
    [SerializeField] private int usesBeforePatternChange = 0;
    [SerializeField] private int usesRemaining = 0;
    [SerializeField] private float currentEggSpeed = 0f;

    // Combat state management
    private CombatState currentState = CombatState.WaitingForChickens;
    private float stateTimer = 0f;
    private BaseChickenAttackSO currentAttack = null;
    private int currentAttackUsesInternal = 0;
    private float nextAttackTime = 0f;
    private bool usingFallbackAttack = false;

    // Difficulty scaling
    private float originalEggSpeed = 0f;
    private float originalPatternCooldown = 0f;
    private Coroutine difficultyScalingCoroutine = null;
    private bool isInEnemyWave = false;

    private Transform player;

    // Combat states enum
    public enum CombatState
    {
        WaitingForChickens,
        PatternCooldown,
        Attacking
    }

    // Public properties
    public int TotalCombatChickens => registeredCombatChickens.Count;
    public List<ChickenCombatBehaviorV2> RegisteredChickens => new List<ChickenCombatBehaviorV2>(registeredCombatChickens);
    public List<ChickenCombatBehaviorV2> GetAvailableAttackers() => GetAvailableAttackersInternal();
    public int AvailableAttackers => GetAvailableAttackersInternal().Count;
    public float EggSpeed => eggSpeed;
    public Transform Player => player;
    public AttackLootTableSO AttackLootTable => attackLootTable;
    public CombatState CurrentState => currentState;
    public BaseChickenAttackSO CurrentAttack => currentAttack;
    public int CurrentAttackUses => currentAttackUsesInternal;
    public bool IsUsingFallbackAttack => usingFallbackAttack;

    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Respawn");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("ChickenCombatManagerV4: No player found with 'Player' tag!");
        }

        // Initialize with default attack loot table if current is null
        if (attackLootTable == null)
        {
            attackLootTable = defaultAttackLootTable;
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Initialized with default attack loot table");
        }

        // Validate attack loot table
        if (attackLootTable == null)
        {
            Debug.LogWarning("ChickenCombatManagerV4: No attack loot table assigned and no default available!");
        }
        else if (showDebugLogs)
        {
            Debug.Log($"ChickenCombatManagerV4: Attack loot table '{attackLootTable.name}' loaded with {attackLootTable.GetValidAttacks().Count} valid attacks");
        }

        // Validate fallback attack
        if (fallbackAttack == null)
        {
            Debug.LogWarning("ChickenCombatManagerV4: No fallback attack assigned! Combat may fail if no attacks can be executed.");
        }
        else if (showDebugLogs)
        {
            Debug.Log($"ChickenCombatManagerV4: Fallback attack '{fallbackAttack.AttackName}' assigned");
        }

        // Store original egg speed
        originalEggSpeed = eggSpeed;
        currentEggSpeed = eggSpeed;

        // Store original pattern cooldown
        originalPatternCooldown = PatternChangeCooldown;

        // Initialize combat state
        ResetCombatState();
        UpdateInspectorFields();

        // Subscribe to level manager stage changes
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnStageChanged += OnStageChanged;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from level manager
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnStageChanged -= OnStageChanged;
        }
    }

    void Update()
    {
        if (!enableCombat)
        {
            if (showDebugLogs && Time.frameCount % 300 == 0)
                Debug.Log("ChickenCombatManagerV4: Combat is disabled");
            return;
        }

        if (player == null)
        {
            if (showDebugLogs && Time.frameCount % 300 == 0)
                Debug.Log("ChickenCombatManagerV4: No player found, cannot attack");
            return;
        }

        // Update combat state machine
        UpdateCombatStateMachine();

        // Update inspector fields for real-time feedback
        UpdateInspectorFields();
    }

    private void UpdateInspectorFields()
    {
        // Update combat status fields for inspector
        if (currentAttack != null)
        {
            currentAttackSO = currentAttack;
            usesBeforePatternChange = currentAttack.UsesBeforePatternChange;
            usesRemaining = Mathf.Max(0, currentAttack.UsesBeforePatternChange - currentAttackUsesInternal);
        }
        else
        {
            currentAttackSO = null;
            usesBeforePatternChange = 0;
            usesRemaining = 0;
        }

        currentEggSpeed = eggSpeed;
    }

    private void OnStageChanged(SOLevelStage newStage)
    {
        if (newStage == null) return;

        // Update attack loot table based on stage
        UpdateAttackLootTableForStage(newStage);

        // Check if we're entering or leaving an enemy wave stage
        bool wasInEnemyWave = isInEnemyWave;
        isInEnemyWave = newStage.StageType == StageType.EnemyWave;

        if (isInEnemyWave)
        {
            // Entering any enemy wave - always reset and start fresh difficulty scaling
            // This ensures each wave starts from the original egg speed
            StartDifficultyScaling();
        }
        else if (wasInEnemyWave)
        {
            // Leaving enemy wave to non-enemy stage - stop difficulty scaling
            StopDifficultyScaling();
        }
    }

    private void UpdateAttackLootTableForStage(SOLevelStage stage)
    {
        if (stage == null) return;

        AttackLootTableSO newLootTable = null;

        // Check if stage has a wave attack table assigned
        if (stage.WaveAttackTable != null)
        {
            newLootTable = stage.WaveAttackTable;
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Using stage-specific attack loot table '{newLootTable.name}'");
        }
        else
        {
            // Use default loot table as fallback
            newLootTable = defaultAttackLootTable;
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Stage has no attack loot table, using default '{(newLootTable != null ? newLootTable.name : "NONE")}'");
        }

        // Only update if the table is different
        if (newLootTable != attackLootTable)
        {
            attackLootTable = newLootTable;

            // Reset combat state when changing loot tables to avoid using attacks from previous table
            if (currentState != CombatState.WaitingForChickens)
            {
                if (showDebugLogs)
                    Debug.Log("ChickenCombatManagerV4: Attack loot table changed, resetting combat state");

                ResetCombatState();
            }

            // Validate the new loot table
            if (attackLootTable == null)
            {
                Debug.LogWarning($"ChickenCombatManagerV4: No attack loot table available for stage '{stage.StageTitle}'!");
            }
            else if (showDebugLogs)
            {
                Debug.Log($"ChickenCombatManagerV4: Updated to attack loot table '{attackLootTable.name}' with {attackLootTable.GetValidAttacks().Count} valid attacks");
            }
        }
    }

    private void StartDifficultyScaling()
    {
        // Stop any existing coroutine
        if (difficultyScalingCoroutine != null)
        {
            StopCoroutine(difficultyScalingCoroutine);
        }

        // Reset to original values
        eggSpeed = originalEggSpeed;
        PatternChangeCooldown = originalPatternCooldown;

        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV4: Starting difficulty scaling. Egg speed: {eggSpeed} (+{eggSpeedIncreaseAmount} every {difficultyIncreaseInterval}s, max: {maxEggSpeed}). Pattern cooldown: {PatternChangeCooldown} (-{patternCooldownDecreaseAmount} every {difficultyIncreaseInterval}s, min: {minPatternCooldown})");

        // Start the scaling coroutine
        difficultyScalingCoroutine = StartCoroutine(DifficultyScalingCoroutine());
    }

    private void StopDifficultyScaling()
    {
        // Stop the coroutine
        if (difficultyScalingCoroutine != null)
        {
            StopCoroutine(difficultyScalingCoroutine);
            difficultyScalingCoroutine = null;
        }

        // Reset to original values
        eggSpeed = originalEggSpeed;
        PatternChangeCooldown = originalPatternCooldown;

        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV4: Stopped difficulty scaling and reset to original values. Egg speed: {eggSpeed}, Pattern cooldown: {PatternChangeCooldown}");
    }

    private IEnumerator DifficultyScalingCoroutine()
    {
        while (true)
        {
            // Wait for the interval
            yield return new WaitForSeconds(difficultyIncreaseInterval);

            // Increase egg speed
            float previousEggSpeed = eggSpeed;
            eggSpeed = Mathf.Min(eggSpeed + eggSpeedIncreaseAmount, maxEggSpeed);

            // Decrease pattern cooldown
            float previousPatternCooldown = PatternChangeCooldown;
            PatternChangeCooldown = Mathf.Max(PatternChangeCooldown - patternCooldownDecreaseAmount, minPatternCooldown);

            if (showDebugLogs)
            {
                bool eggSpeedChanged = !Mathf.Approximately(previousEggSpeed, eggSpeed);
                bool cooldownChanged = !Mathf.Approximately(previousPatternCooldown, PatternChangeCooldown);

                if (eggSpeedChanged || cooldownChanged)
                {
                    Debug.Log($"ChickenCombatManagerV4: Difficulty increased! " +
                             $"Egg speed: {previousEggSpeed:F1} → {eggSpeed:F1} (max: {maxEggSpeed}), " +
                             $"Pattern cooldown: {previousPatternCooldown:F1}s → {PatternChangeCooldown:F1}s (min: {minPatternCooldown}s)");
                }
                else
                {
                    Debug.Log("ChickenCombatManagerV4: Difficulty at maximum values");
                }
            }

            // Stop if we've reached both maximums
            if (eggSpeed >= maxEggSpeed && PatternChangeCooldown <= minPatternCooldown)
            {
                if (showDebugLogs)
                    Debug.Log("ChickenCombatManagerV4: Maximum difficulty reached, stopping scaling");
                yield break;
            }
        }
    }

    private void UpdateCombatStateMachine()
    {
        switch (currentState)
        {
            case CombatState.WaitingForChickens:
                HandleWaitingForChickens();
                break;

            case CombatState.PatternCooldown:
                HandlePatternCooldown();
                break;

            case CombatState.Attacking:
                HandleAttacking();
                break;
        }
    }

    private void HandleWaitingForChickens()
    {
        if (TotalCombatChickens > 0)
        {
            // Chickens are registered, start pattern cooldown
            StartPatternCooldown();
        }
    }

    private void StartPatternCooldown()
    {
        currentState = CombatState.PatternCooldown;
        currentAttack = null;
        currentAttackUsesInternal = 0;
        stateTimer = PatternChangeCooldown;
        usingFallbackAttack = false;

        if (showDebugLogs)
            Debug.Log($"ChickenCombatManagerV4: Starting pattern cooldown ({PatternChangeCooldown}s)");
    }

    private void HandlePatternCooldown()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            // Cooldown complete, select new attack
            SelectNewAttack();
        }
    }

    private void HandleAttacking()
    {
        if (currentAttack == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("ChickenCombatManagerV4: In attacking state but no attack selected");
            StartPatternCooldown();
            return;
        }

        // Check if we need to wait before next attack
        if (Time.time < nextAttackTime)
        {
            return;
        }

        // Check if we've reached the use limit
        if (currentAttackUsesInternal >= currentAttack.UsesBeforePatternChange)
        {
            string attackSource = usingFallbackAttack ? " (fallback)" : "";
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Attack '{currentAttack.AttackName}'{attackSource} reached use limit ({currentAttackUsesInternal}/{currentAttack.UsesBeforePatternChange})");

            StartPatternCooldown();
            return;
        }

        // Execute attack
        ExecuteCurrentAttack();
    }

    private void SelectNewAttack()
    {
        var availableChickens = GetAvailableAttackersInternal();
        if (availableChickens.Count == 0)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV4: No available attackers, waiting...");

            currentState = CombatState.WaitingForChickens;
            return;
        }

        // Try to select a random attack from the loot table
        BaseChickenAttackSO selectedAttack = SelectRandomAttack();

        if (selectedAttack == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("ChickenCombatManagerV4: Failed to select attack from loot table, trying fallback");

            UseFallbackAttack(availableChickens);
            return;
        }

        // Check if the selected attack can be executed
        if (selectedAttack.CanExecute(availableChickens, this))
        {
            currentAttack = selectedAttack;
            currentAttackUsesInternal = 0;
            currentState = CombatState.Attacking;
            nextAttackTime = Time.time;
            usingFallbackAttack = false;

            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Selected attack '{currentAttack.AttackName}' (Type: {currentAttack.AttackType}, Uses: {currentAttack.UsesBeforePatternChange})");
        }
        else
        {
            // Selected attack can't be executed, try selecting another
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Selected attack '{selectedAttack.AttackName}' cannot be executed, trying another");

            // Try to get a different attack type
            BaseChickenAttackSO alternativeAttack = SelectRandomAttackExcluding(selectedAttack.AttackType);

            if (alternativeAttack != null && alternativeAttack.CanExecute(availableChickens, this))
            {
                currentAttack = alternativeAttack;
                currentAttackUsesInternal = 0;
                currentState = CombatState.Attacking;
                nextAttackTime = Time.time;
                usingFallbackAttack = false;

                if (showDebugLogs)
                    Debug.Log($"ChickenCombatManagerV4: Selected alternative attack '{currentAttack.AttackName}' (Type: {currentAttack.AttackType})");
            }
            else
            {
                // No viable attacks from loot table, use fallback
                if (showDebugLogs)
                    Debug.Log("ChickenCombatManagerV4: No viable attacks from loot table, using fallback");

                UseFallbackAttack(availableChickens);
            }
        }
    }

    private void UseFallbackAttack(List<ChickenCombatBehaviorV2> availableChickens)
    {
        if (fallbackAttack == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("ChickenCombatManagerV4: No fallback attack available, starting cooldown");

            StartPatternCooldown();
            return;
        }

        // Check if fallback attack can be executed
        if (fallbackAttack.CanExecute(availableChickens, this))
        {
            currentAttack = fallbackAttack;
            currentAttackUsesInternal = 0;
            currentState = CombatState.Attacking;
            nextAttackTime = Time.time;
            usingFallbackAttack = true;

            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Using fallback attack '{currentAttack.AttackName}' (Type: {currentAttack.AttackType})");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("ChickenCombatManagerV4: Even fallback attack cannot be executed, starting cooldown");

            StartPatternCooldown();
        }
    }

    private void ExecuteCurrentAttack()
    {
        if (currentAttack == null) return;

        var availableChickens = GetAvailableAttackersInternal();
        if (availableChickens.Count == 0)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV4: No available attackers for execution");
            return;
        }

        // Check if attack can still be executed
        if (!currentAttack.CanExecute(availableChickens, this))
        {
            string attackSource = usingFallbackAttack ? " (fallback)" : "";
            if (showDebugLogs)
                Debug.Log($"ChickenCombatManagerV4: Attack '{currentAttack.AttackName}'{attackSource} can no longer be executed");

            StartPatternCooldown();
            return;
        }

        // Execute the attack
        currentAttack.Execute(availableChickens, this);
        currentAttackUsesInternal++;

        // Set next attack time
        nextAttackTime = Time.time + currentAttack.AttackInterval;

        if (showDebugLogs)
        {
            string attackSource = usingFallbackAttack ? " (fallback)" : "";
            Debug.Log($"ChickenCombatManagerV4: Executed '{currentAttack.AttackName}'{attackSource} (Use {currentAttackUsesInternal}/{currentAttack.UsesBeforePatternChange})");
        }
    }

    private void ResetCombatState()
    {
        currentState = CombatState.WaitingForChickens;
        currentAttack = null;
        currentAttackUsesInternal = 0;
        stateTimer = 0f;
        nextAttackTime = 0f;
        usingFallbackAttack = false;

        if (showDebugLogs)
            Debug.Log("ChickenCombatManagerV4: Combat state reset to WaitingForChickens");
    }

    List<ChickenCombatBehaviorV2> GetAvailableAttackersInternal()
    {
        List<ChickenCombatBehaviorV2> available = new List<ChickenCombatBehaviorV2>();

        foreach (ChickenCombatBehaviorV2 chicken in registeredCombatChickens)
        {
            if (!chicken) continue;

            if (chicken.IsReadyToAttack)
            {
                available.Add(chicken);
            }
        }

        return available;
    }

    // ATTACK SELECTION METHODS
    public BaseChickenAttackSO SelectRandomAttack()
    {
        if (attackLootTable == null)
        {
            Debug.LogWarning("ChickenCombatManagerV4: No attack loot table assigned, cannot select attack");
            return null;
        }

        return attackLootTable.SelectRandomAttack();
    }

    public BaseChickenAttackSO SelectRandomAttackExcluding(AttackType excludeType)
    {
        if (attackLootTable == null)
        {
            Debug.LogWarning("ChickenCombatManagerV4: No attack loot table assigned, cannot select attack");
            return null;
        }

        return attackLootTable.SelectRandomAttackThatIsNot(excludeType);
    }

    // REGISTRATION SYSTEM
    public bool RegisterChickenForCombat(ChickenCombatBehaviorV2 chicken)
    {
        if (chicken == null)
        {
            if (showRegistrationLogs)
                Debug.LogWarning("ChickenCombatManagerV4: Attempted to register null chicken for combat");
            return false;
        }

        if (registeredCombatChickens.Contains(chicken))
        {
            if (showRegistrationLogs)
                Debug.Log($"ChickenCombatManagerV4: Chicken {chicken.gameObject.name} already registered for combat");
            return false;
        }

        registeredCombatChickens.Add(chicken);

        if (showRegistrationLogs)
            Debug.Log($"ChickenCombatManagerV4: Registered chicken {chicken.gameObject.name} for combat ({registeredCombatChickens.Count} total)");

        // Check if this is the first chicken and we should start combat
        if (currentState == CombatState.WaitingForChickens && TotalCombatChickens == 1)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV4: First chicken registered, starting combat system");
        }

        return true;
    }

    public bool UnregisterChickenFromCombat(ChickenCombatBehaviorV2 chicken)
    {
        if (chicken == null) return false;

        bool wasRegistered = registeredCombatChickens.Remove(chicken);

        if (wasRegistered && showRegistrationLogs)
            Debug.Log($"ChickenCombatManagerV4: Unregistered chicken {chicken.gameObject.name} from combat ({registeredCombatChickens.Count} total)");

        // If no chickens left, reset combat state
        if (TotalCombatChickens == 0 && currentState != CombatState.WaitingForChickens)
        {
            if (showDebugLogs)
                Debug.Log("ChickenCombatManagerV4: No chickens left, resetting combat state");

            ResetCombatState();
        }

        return wasRegistered;
    }

    // Keep this method for backwards compatibility but mark it as obsolete
    [System.Obsolete("Use RegisterChickenForCombat instead. This method is kept for backwards compatibility.")]
    public void RegisterChicken(ChickenCombatBehaviorV2 chicken)
    {
        RegisterChickenForCombat(chicken);
    }

    // Keep this method for backwards compatibility but mark it as obsolete  
    [System.Obsolete("Use UnregisterChickenFromCombat instead. This method is kept for backwards compatibility.")]
    public void UnregisterChicken(ChickenCombatBehaviorV2 chicken)
    {
        UnregisterChickenFromCombat(chicken);
    }

    void OnDrawGizmos()
    {
        if (!showAttackGizmos) return;

        // Show combat state
        Gizmos.color = GetStateColor();
        if (transform.position != Vector3.zero)
        {
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);

            // Show fallback indicator if using fallback attack
            if (usingFallbackAttack && currentState == CombatState.Attacking)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 4f, Vector3.one * 0.3f);
            }
        }

        if (registeredCombatChickens.Count > 0)
        {
            foreach (var chicken in registeredCombatChickens)
            {
                if (chicken != null)
                {
                    // Show chicken readiness
                    Gizmos.color = chicken.IsReadyToAttack ? Color.green : Color.red;
                    Gizmos.DrawWireSphere(chicken.transform.position + Vector3.up * 2f, 0.5f);
                }
            }
        }
    }

    private Color GetStateColor()
    {
        switch (currentState)
        {
            case CombatState.WaitingForChickens: return Color.yellow;
            case CombatState.PatternCooldown: return Color.blue;
            case CombatState.Attacking: return Color.red;
            default: return Color.white;
        }
    }
}