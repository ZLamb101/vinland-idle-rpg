using System;

/// <summary>
/// Base class for all game events
/// Events carry data and can be sent through the EventBus
/// </summary>
public abstract class GameEvent
{
    public DateTime timestamp = DateTime.Now;
}

// ==================== Character Events ====================

public class CharacterXPChangedEvent : GameEvent
{
    public int newXP;
    public int xpGained;
}

public class CharacterLevelChangedEvent : GameEvent
{
    public int newLevel;
}

public class CharacterLevelUpEvent : GameEvent
{
    public int oldLevel;
    public int newLevel;
}

public class CharacterGoldChangedEvent : GameEvent
{
    public int newGold;
    public int goldChanged; // positive = gained, negative = spent
}

public class CharacterNameChangedEvent : GameEvent
{
    public string newName;
}

public class CharacterHealthChangedEvent : GameEvent
{
    public float currentHealth;
    public float maxHealth;
    public float healthChanged; // positive = healed, negative = damaged
}

public class CharacterDiedEvent : GameEvent
{
    public string deathReason;
}

// ==================== Combat Events ====================

public class CombatStateChangedEvent : GameEvent
{
    public CombatManager.CombatState newState;
    public CombatManager.CombatState oldState;
}

public class CombatStartedEvent : GameEvent
{
    public MonsterData[] monsters;
    public int mobCount;
}

public class CombatEndedEvent : GameEvent
{
    public bool wasVictory;
    public int monstersDefeated;
}

public class PlayerHealthChangedEvent : GameEvent
{
    public float currentHealth;
    public float maxHealth;
}

public class MonsterHealthChangedEvent : GameEvent
{
    public float currentHealth;
    public float maxHealth;
    public int monsterIndex;
}

public class MonstersChangedEvent : GameEvent
{
    public System.Collections.Generic.List<MonsterData> monsters;
}

public class TargetChangedEvent : GameEvent
{
    public int newTargetIndex;
}

public class MonsterSpawnedEvent : GameEvent
{
    public MonsterData monsterData;
    public int monsterIndex;
}

public class MonsterDiedEvent : GameEvent
{
    public MonsterData monsterData;
    public int monsterIndex;
    public int xpAwarded;
    public int goldAwarded;
}

public class PlayerAttackProgressEvent : GameEvent
{
    public float progress; // 0 to 1
}

public class MonsterAttackProgressEvent : GameEvent
{
    public float progress; // 0 to 1
    public int monsterIndex;
}

public class PlayerDamageDealtEvent : GameEvent
{
    public float damage;
    public bool wasCritical;
}

public class PlayerDamageTakenEvent : GameEvent
{
    public float damage;
}

public class DamageDealtEvent : GameEvent
{
    public float damage;
    public bool wasCritical;
    public string source; // "Player" or "Monster"
    public int targetIndex = -1; // For monster targets
}

// ==================== Inventory Events ====================

public class ItemAddedEvent : GameEvent
{
    public InventoryItem item;
    public int quantity;
    public bool wasSuccessful;
}

public class ItemRemovedEvent : GameEvent
{
    public InventoryItem item;
    public int quantity;
    public int slotIndex;
}

public class InventoryFullEvent : GameEvent
{
    public InventoryItem attemptedItem;
    public int itemsLost;
}

// ==================== Equipment Events ====================

public class EquipmentChangedEvent : GameEvent
{
    public EquipmentSlot slot;
    public EquipmentData oldEquipment;
    public EquipmentData newEquipment;
}

public class StatsRecalculatedEvent : GameEvent
{
    public EquipmentStats equipmentStats;
    public TalentBonuses talentBonuses;
}

// ==================== Shop Events ====================

public class ShopOpenedEvent : GameEvent
{
    public ShopData shop;
}

public class ShopClosedEvent : GameEvent
{
    public ShopData shop;
}

public class ShopStockChangedEvent : GameEvent
{
    public int itemIndex;
}

public class ShopBuyBackChangedEvent : GameEvent
{
}

public class ItemPurchasedEvent : GameEvent
{
    public ItemData item;
    public int quantity;
    public int goldSpent;
}

public class ItemSoldEvent : GameEvent
{
    public InventoryItem item;
    public int goldEarned;
}

// ==================== Zone Events ====================

public class ZoneChangedEvent : GameEvent
{
    public ZoneData oldZone;
    public ZoneData newZone;
}

public class QuestsUpdatedEvent : GameEvent
{
    public QuestData[] availableQuests;
}

// ==================== Dialogue Events ====================

public class DialogueStartedEvent : GameEvent
{
    public NPCData npc;
}

public class DialogueTextChangedEvent : GameEvent
{
    public string dialogueText;
}

public class DialogueEndedEvent : GameEvent
{
}

// ==================== Talent Events ====================

public class TalentUnlockedEvent : GameEvent
{
    public TalentData talent;
    public int newRank;
    public int talentPointsRemaining;
}

public class TalentPointsChangedEvent : GameEvent
{
    public int unspentPoints;
    public int totalPoints;
}

public class TalentBonusesRecalculatedEvent : GameEvent
{
    public TalentBonuses talentBonuses;
}

// ==================== Resource Events ====================

public class GatheringStateChangedEvent : GameEvent
{
    public bool isGathering;
    public ResourceData resource;
}

public class ResourceChangedEvent : GameEvent
{
    public ResourceData resource;
}

public class GatherProgressChangedEvent : GameEvent
{
    public float progress; // 0 to 1
}

public class ItemsGatheredEvent : GameEvent
{
    public int itemsGathered;
    public ResourceData resource;
}

public class GatheringStartedEvent : GameEvent
{
    public ResourceData resource;
}

public class GatheringStoppedEvent : GameEvent
{
    public ResourceData resource;
}

public class ResourceGatheredEvent : GameEvent
{
    public ResourceData resource;
    public int quantity;
}

// ==================== Away Activity Events ====================

public class ActivityStartedEvent : GameEvent
{
    public AwayActivityType activityType;
    public string activityName;
}

public class ActivityStoppedEvent : GameEvent
{
    public AwayActivityType activityType;
}

public class AwayRewardsCalculatedEvent : GameEvent
{
    public AwayRewards rewards;
}

// ==================== UI Events ====================

public class TooltipShowEvent : GameEvent
{
    public string title;
    public string description;
}

public class TooltipHideEvent : GameEvent
{
}

public class NotificationEvent : GameEvent
{
    public string message;
    public NotificationType type;
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

