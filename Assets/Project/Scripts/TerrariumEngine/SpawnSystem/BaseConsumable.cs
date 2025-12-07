using UnityEngine;
using TerrariumEngine.AI;

namespace TerrariumEngine.SpawnSystem
{
    /// <summary>
    /// Base class for all consumable items (Food, Water, etc.)
    /// Provides common functionality for consumption, nutrition, lifecycle management, and picking
    /// </summary>
    public abstract class BaseConsumable : MonoBehaviour, IDebuggable, IPickable
    {
        [Header("Consumable Settings")]
        [SerializeField] protected string itemName = "Consumable";
        [SerializeField] protected float maxAmount = 100f;
        [SerializeField] protected float currentAmount = 100f;
        [SerializeField] protected float consumptionRate = 1f; // Amount consumed per use
        [SerializeField] protected bool isConsumable = true;
        [SerializeField] protected bool destroyWhenEmpty = true;
        
        [Header("Lifetime & Rot Settings")]
        [SerializeField] protected float maxLifetime = 300f; // Maximum lifetime in seconds (5 minutes default)
        [SerializeField] protected float currentLifetime = 0f; // Current age of the item
        [SerializeField] protected float rotRate = 0.1f; // How fast the item rots (per second)
        [SerializeField] protected float maxRot = 1f; // Maximum rot level (0 = fresh, 1 = completely rotten)
        [SerializeField] protected float currentRot = 0f; // Current rot level
        [SerializeField] protected bool canRot = true; // Whether this item can rot
        [SerializeField] protected bool destroyWhenRotten = true; // Whether to destroy when completely rotten
        
        [Header("Pickable Settings")]
        [SerializeField] protected bool canBePicked = true;
        [SerializeField] protected float itemWeight = 1f; // Weight of the item
        [SerializeField] protected ItemSize itemSize = ItemSize.Small;
        [SerializeField] protected Transform pickupAttachmentPoint; // Where to attach when picked up
        
        [Header("Nutrition/Effect Settings")]
        [SerializeField] protected float nutritionValue = 10f; // How much this item satisfies hunger/thirst
        [SerializeField] protected float healthBonus = 0f; // Health restoration when consumed
        [SerializeField] protected float stressReduction = 0f; // Stress reduction when consumed
        
        [Header("Visual Settings")]
        [SerializeField] protected bool scaleWithAmount = true;
        [SerializeField] protected Vector3 minScale = Vector3.one * 0.1f;
        [SerializeField] protected Vector3 maxScale = Vector3.one;
        [SerializeField] protected bool changeColorWithAmount = true;
        [SerializeField] protected Color fullColor = Color.white;
        [SerializeField] protected Color emptyColor = Color.gray;
        
        [Header("Debug")]
        [SerializeField] protected bool showDebugInfo = true;
        
        public string DebugName => $"{GetType().Name}_{itemName}";
        public bool IsDebugEnabled { get; set; } = true;
        
        // Properties
        public string ItemName => itemName;
        public float MaxAmount => maxAmount;
        public float CurrentAmount => currentAmount;
        public float AmountPercentage => maxAmount > 0 ? currentAmount / maxAmount : 0f;
        public bool IsEmpty => currentAmount <= 0f;
        public bool IsFull => currentAmount >= maxAmount;
        public float NutritionValue => nutritionValue;
        public float HealthBonus => healthBonus;
        public float StressReduction => stressReduction;
        
        // Lifetime & Rot Properties
        public float MaxLifetime => maxLifetime;
        public float CurrentLifetime => currentLifetime;
        public float LifetimePercentage => maxLifetime > 0 ? currentLifetime / maxLifetime : 0f;
        public float CurrentRot => currentRot;
        public float MaxRot => maxRot;
        public float RotPercentage => maxRot > 0 ? currentRot / maxRot : 0f;
        public bool IsRotten => currentRot >= maxRot;
        public bool IsFresh => currentRot <= 0.1f;
        public bool CanRot => canRot;
        
        // Pickable Properties
        public bool CanBePicked => canBePicked;
        public float Weight => itemWeight;
        public ItemSize Size => itemSize;
        
        // Picking state
        protected bool isPickedUp = false;
        protected IPicker currentPicker = null;
        protected Vector3 originalPosition;
        protected Transform originalParent;
        
        // Events
        public System.Action<BaseConsumable, float> OnAmountChanged;
        public System.Action<BaseConsumable, BaseNPC> OnConsumed;
        public System.Action<BaseConsumable> OnEmpty;
        public System.Action<BaseConsumable> OnDestroyed;
        public System.Action<BaseConsumable, float> OnRotChanged;
        public System.Action<BaseConsumable> OnRotten;
        public System.Action<BaseConsumable, IPicker> OnItemPickedUp;
        public System.Action<BaseConsumable, IPicker> OnItemDropped;
        
        protected Renderer itemRenderer;
        protected Collider itemCollider;
        protected Rigidbody itemRigidbody;
        
        protected virtual void Awake()
        {
            // Get or add required components
            itemRenderer = GetComponent<Renderer>();
            if (itemRenderer == null)
            {
                itemRenderer = GetComponentInChildren<Renderer>();
            }
            
            itemCollider = GetComponent<Collider>();
            if (itemCollider == null)
            {
                itemCollider = GetComponentInChildren<Collider>();
            }
            
            itemRigidbody = GetComponent<Rigidbody>();
            if (itemRigidbody == null)
            {
                itemRigidbody = GetComponentInChildren<Rigidbody>();
            }
            
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
        }
        
        protected virtual void Start()
        {
            // Initialize amount
            currentAmount = maxAmount;
            
            // Initialize lifetime and rot
            currentLifetime = 0f;
            currentRot = 0f;
            
            // Store original state for picking
            originalPosition = transform.position;
            originalParent = transform.parent;
            
            // Update visual representation
            UpdateVisuals();
            
            // Initialize specific consumable type
            InitializeConsumable();
        }
        
        protected virtual void Update()
        {
            // Update lifetime
            currentLifetime += Time.deltaTime;
            
            // Update rot if item can rot
            if (canRot && !IsEmpty)
            {
                UpdateRot();
            }
            
            // Check if item should be destroyed due to lifetime
            if (maxLifetime > 0 && currentLifetime >= maxLifetime)
            {
                HandleLifetimeExpired();
            }
        }
        
        protected virtual void OnDestroy()
        {
            OnDestroyed?.Invoke(this);
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        /// <summary>
        /// Initialize specific consumable type - override in derived classes
        /// </summary>
        protected abstract void InitializeConsumable();
        
        /// <summary>
        /// Update rot over time
        /// </summary>
        protected virtual void UpdateRot()
        {
            float rotIncrease = rotRate * Time.deltaTime;
            SetRot(currentRot + rotIncrease);
        }
        
        /// <summary>
        /// Set the rot level
        /// </summary>
        /// <param name="newRot">New rot level</param>
        public virtual void SetRot(float newRot)
        {
            float oldRot = currentRot;
            currentRot = Mathf.Clamp(newRot, 0f, maxRot);
            
            if (currentRot != oldRot)
            {
                OnRotChanged?.Invoke(this, currentRot - oldRot);
                UpdateVisuals();
                
                if (IsRotten)
                {
                    OnRotten?.Invoke(this);
                    HandleRot();
                }
            }
        }
        
        /// <summary>
        /// Handle rot effects - override in derived classes
        /// </summary>
        protected virtual void HandleRot()
        {
            // Reduce nutrition value when rotten
            nutritionValue *= 0.1f; // Only 10% nutrition when rotten
            
            // Change color to indicate rot
            if (itemRenderer != null)
            {
                itemRenderer.material.color = Color.brown;
            }
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Item has rotted!");
            }
            
            // Destroy if configured to do so
            if (destroyWhenRotten)
            {
                DestroyConsumable();
            }
        }
        
        /// <summary>
        /// Handle lifetime expiration
        /// </summary>
        protected virtual void HandleLifetimeExpired()
        {
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Lifetime expired after {currentLifetime:F1} seconds");
            }
            
            // Set to maximum rot and destroy
            SetRot(maxRot);
        }
        
        /// <summary>
        /// Consume this item by an NPC
        /// </summary>
        /// <param name="consumer">NPC consuming the item</param>
        /// <param name="amount">Amount to consume (defaults to consumption rate)</param>
        /// <returns>Amount actually consumed</returns>
        public virtual float Consume(BaseNPC consumer, float amount = -1f)
        {
            if (!isConsumable || IsEmpty || consumer == null)
            {
                return 0f;
            }
            
            // Use consumption rate if amount not specified
            if (amount < 0f)
            {
                amount = consumptionRate;
            }
            
            // Don't consume more than available
            float actualAmount = Mathf.Min(amount, currentAmount);
            
            // Reduce amount
            SetAmount(currentAmount - actualAmount);
            
            // Apply effects to consumer
            ApplyConsumptionEffects(consumer, actualAmount);
            
            // Fire consumption event
            OnConsumed?.Invoke(this, consumer);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Consumed {actualAmount} by {consumer.DebugName}. Remaining: {currentAmount}");
            }
            
            // Check if empty and should be destroyed
            if (IsEmpty && destroyWhenEmpty)
            {
                DestroyConsumable();
            }
            
            return actualAmount;
        }
        
        /// <summary>
        /// Apply consumption effects to the consumer - override in derived classes
        /// </summary>
        /// <param name="consumer">NPC that consumed the item</param>
        /// <param name="amount">Amount consumed</param>
        protected virtual void ApplyConsumptionEffects(BaseNPC consumer, float amount)
        {
            // Apply health bonus
            if (healthBonus > 0f)
            {
                consumer.Heal(healthBonus * amount);
            }
            
            // Apply stress reduction
            if (stressReduction > 0f)
            {
                consumer.ModifyStat(StatType.Stress, -stressReduction * amount);
            }
        }
        
        /// <summary>
        /// Set the current amount of the consumable
        /// </summary>
        /// <param name="newAmount">New amount</param>
        public virtual void SetAmount(float newAmount)
        {
            float oldAmount = currentAmount;
            currentAmount = Mathf.Clamp(newAmount, 0f, maxAmount);
            
            if (currentAmount != oldAmount)
            {
                OnAmountChanged?.Invoke(this, currentAmount - oldAmount);
                UpdateVisuals();
                
                if (IsEmpty)
                {
                    OnEmpty?.Invoke(this);
                }
            }
        }
        
        /// <summary>
        /// Add amount to the consumable
        /// </summary>
        /// <param name="amount">Amount to add</param>
        public virtual void AddAmount(float amount)
        {
            SetAmount(currentAmount + amount);
        }
        
        /// <summary>
        /// Refill the consumable to maximum
        /// </summary>
        public virtual void Refill()
        {
            SetAmount(maxAmount);
        }
        
        /// <summary>
        /// Check if an NPC can consume this item
        /// </summary>
        /// <param name="consumer">NPC to check</param>
        /// <returns>True if can consume</returns>
        public virtual bool CanBeConsumedBy(BaseNPC consumer)
        {
            return isConsumable && !IsEmpty && !IsRotten && consumer != null && consumer.IsAlive;
        }
        
        /// <summary>
        /// Get the consumption priority for an NPC (higher = more important)
        /// Override in derived classes for specific logic
        /// </summary>
        /// <param name="consumer">NPC to check priority for</param>
        /// <returns>Priority value</returns>
        public virtual float GetConsumptionPriority(BaseNPC consumer)
        {
            if (!CanBeConsumedBy(consumer))
            {
                return 0f;
            }
            
            // Base priority is nutrition value
            return nutritionValue;
        }
        
        /// <summary>
        /// Update visual representation based on current amount
        /// </summary>
        protected virtual void UpdateVisuals()
        {
            if (itemRenderer == null) return;
            
            float percentage = AmountPercentage;
            
            // Update scale
            if (scaleWithAmount)
            {
                Vector3 scale = Vector3.Lerp(minScale, maxScale, percentage);
                transform.localScale = scale;
            }
            
            // Update color
            if (changeColorWithAmount)
            {
                Color currentColor = Color.Lerp(emptyColor, fullColor, percentage);
                itemRenderer.material.color = currentColor;
            }
        }
        
        /// <summary>
        /// Destroy this consumable
        /// </summary>
        protected virtual void DestroyConsumable()
        {
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Destroyed (empty)");
            }
            
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Get information about this consumable for NPCs
        /// </summary>
        /// <returns>Consumable info</returns>
        public virtual ConsumableInfo GetConsumableInfo()
        {
            return new ConsumableInfo
            {
                itemName = itemName,
                currentAmount = currentAmount,
                maxAmount = maxAmount,
                nutritionValue = nutritionValue,
                healthBonus = healthBonus,
                stressReduction = stressReduction,
                position = transform.position,
                isConsumable = isConsumable,
                currentLifetime = currentLifetime,
                maxLifetime = maxLifetime,
                currentRot = currentRot,
                maxRot = maxRot,
                weight = itemWeight,
                size = itemSize,
                canBePicked = canBePicked
            };
        }
        
        #region IPickable Implementation
        
        public Transform PickerTransform => pickupAttachmentPoint != null ? pickupAttachmentPoint : transform;
        
        /// <summary>
        /// Try to pick up this item
        /// </summary>
        /// <param name="picker">Entity trying to pick up the item</param>
        /// <returns>True if successfully picked up</returns>
        public virtual bool TryPickup(IPicker picker)
        {
            if (!CanBePickedBy(picker) || isPickedUp)
            {
                return false;
            }
            
            // Check if picker can carry this item
            if (picker.CurrentCarryWeight + itemWeight > picker.MaxCarryWeight)
            {
                if (IsDebugEnabled)
                {
                    Debug.Log($"{DebugName}: Too heavy for {picker} to carry");
                }
                return false;
            }
            
            // Check size restrictions
            if (itemSize > picker.MaxPickupSize)
            {
                if (IsDebugEnabled)
                {
                    Debug.Log($"{DebugName}: Too large for {picker} to pick up");
                }
                return false;
            }
            
            // Perform pickup
            isPickedUp = true;
            currentPicker = picker;
            
            // Disable physics
            if (itemRigidbody != null)
            {
                itemRigidbody.isKinematic = true;
                itemRigidbody.linearVelocity = Vector3.zero;
                itemRigidbody.angularVelocity = Vector3.zero;
            }
            
            // Disable collider
            if (itemCollider != null)
            {
                itemCollider.enabled = false;
            }
            
            // Attach to picker
            Transform attachPoint = pickupAttachmentPoint != null ? pickupAttachmentPoint : picker.PickerTransform;
            transform.SetParent(attachPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            
            OnItemPickedUp?.Invoke(this, picker);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Picked up by {picker}");
            }
            
            return true;
        }
        
        /// <summary>
        /// Called when item is picked up
        /// </summary>
        /// <param name="picker">Entity that picked up the item</param>
        public virtual void OnPickedUp(IPicker picker)
        {
            // Override in derived classes for specific pickup behavior
        }
        
        /// <summary>
        /// Called when item is dropped
        /// </summary>
        /// <param name="picker">Entity that dropped the item</param>
        public virtual void OnDropped(IPicker picker)
        {
            if (!isPickedUp || currentPicker != picker)
            {
                return;
            }
            
            // Restore physics
            if (itemRigidbody != null)
            {
                itemRigidbody.isKinematic = false;
            }
            
            // Re-enable collider
            if (itemCollider != null)
            {
                itemCollider.enabled = true;
            }
            
            // Detach from picker
            transform.SetParent(originalParent);
            transform.position = originalPosition;
            
            // Reset state
            isPickedUp = false;
            currentPicker = null;
            
            OnItemDropped?.Invoke(this, picker);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Dropped by {picker}");
            }
        }
        
        /// <summary>
        /// Check if a specific entity can pick up this item
        /// </summary>
        /// <param name="picker">Entity to check</param>
        /// <returns>True if entity can pick up this item</returns>
        public virtual bool CanBePickedBy(IPicker picker)
        {
            if (!canBePicked || isPickedUp || picker == null)
            {
                return false;
            }
            
            // Check weight
            if (picker.CurrentCarryWeight + itemWeight > picker.MaxCarryWeight)
            {
                return false;
            }
            
            // Check size
            if (itemSize > picker.MaxPickupSize)
            {
                return false;
            }
            
            return true;
        }
        
        #endregion
        
        public virtual void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        protected virtual void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw consumable info above the item
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screenPos.z > 0)
            {
                float yOffset = 0f;
                float labelHeight = 16f;
                
                // Item name
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), itemName);
                yOffset += labelHeight;
                
                // Amount
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Amount: {currentAmount:F1}/{maxAmount:F1} ({AmountPercentage:P0})");
                yOffset += labelHeight;
                
                // Lifetime
                if (maxLifetime > 0)
                {
                    GUI.color = LifetimePercentage > 0.8f ? Color.red : LifetimePercentage > 0.6f ? Color.yellow : Color.white;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Age: {currentLifetime:F1}s ({LifetimePercentage:P0})");
                    GUI.color = Color.white;
                    yOffset += labelHeight;
                }
                
                // Rot
                if (canRot)
                {
                    GUI.color = IsRotten ? Color.red : IsFresh ? Color.green : Color.yellow;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Rot: {RotPercentage:P0}");
                    GUI.color = Color.white;
                    yOffset += labelHeight;
                }
                
                // Weight and size
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Weight: {itemWeight:F1}kg, Size: {itemSize}");
                yOffset += labelHeight;
                
                // Picked up status
                if (isPickedUp)
                {
                    GUI.color = Color.cyan;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        "PICKED UP");
                    GUI.color = Color.white;
                    yOffset += labelHeight;
                }
                
                // Nutrition value
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Nutrition: {nutritionValue:F1}");
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!IsDebugEnabled) return;
            
            // Draw consumption range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
            
            // Draw amount indicator
            Gizmos.color = Color.Lerp(emptyColor, fullColor, AmountPercentage);
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.2f);
        }
    }
    
    /// <summary>
    /// Information about a consumable item for NPCs
    /// </summary>
    [System.Serializable]
    public struct ConsumableInfo
    {
        public string itemName;
        public float currentAmount;
        public float maxAmount;
        public float nutritionValue;
        public float healthBonus;
        public float stressReduction;
        public Vector3 position;
        public bool isConsumable;
        
        // Lifetime and rot info
        public float currentLifetime;
        public float maxLifetime;
        public float currentRot;
        public float maxRot;
        
        // Pickable info
        public float weight;
        public ItemSize size;
        public bool canBePicked;
        
        public float AmountPercentage => maxAmount > 0 ? currentAmount / maxAmount : 0f;
        public bool IsEmpty => currentAmount <= 0f;
        public bool IsFull => currentAmount >= maxAmount;
        public float LifetimePercentage => maxLifetime > 0 ? currentLifetime / maxLifetime : 0f;
        public float RotPercentage => maxRot > 0 ? currentRot / maxRot : 0f;
        public bool IsRotten => currentRot >= maxRot;
        public bool IsFresh => currentRot <= 0.1f;
    }
}
