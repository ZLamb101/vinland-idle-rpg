using System;
using UnityEngine;

/// <summary>
/// Base class for all game events
/// Events carry data and can be sent through the EventBus
/// </summary>
public abstract class GameEvent
{
    public DateTime timestamp = DateTime.Now;
}

// ==================== Quest Events ====================

public class QuestAcceptedEvent : GameEvent
{
    public QuestData quest;
}

public class QuestObjectiveUpdatedEvent : GameEvent
{
    public QuestData quest;
    public int objectiveIndex;
    public int currentAmount;
    public int targetAmount;
}

public class QuestCompletedEvent : GameEvent
{
    public QuestData quest;
}

public class QuestTurnedInEvent : GameEvent
{
    public QuestData quest;
    public int xpReward;
    public int goldReward;
    public ItemData itemReward;
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
    public int goldChanged; 
}

public class CharacterNameChangedEvent : GameEvent
{
    public string newName;
}

public class CharacterHealthChangedEvent : GameEvent
{
    public float currentHealth;
    public float maxHealth;
    public float healthChanged; 
}

public class CharacterDiedEvent : GameEvent
{
    public string deathReason;
}

public class CharacterManaChangedEvent : GameEvent
{
    public float currentMana;
    public float maxMana;
    public float manaChanged; 
}

public class CharacterLoadedEvent : GameEvent
{
    public int slotIndex;
    public string characterName;
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
    public bool wasMiss;
}

public class PlayerDamageTakenEvent : GameEvent
{
    public float damage;
    public bool wasMiss;
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

public class DialogueWithActionsStartedEvent : GameEvent
{
    public NPCData npc;
    public bool hasActions;
    public string acceptButtonText = "Accept";
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

// ==================== Profession Events ====================

public class ProfessionXPGainedEvent : GameEvent
{
    public ProfessionType profession;
    public int xpGained;
    public int currentXP;
    public int xpRequired;
}

public class ProfessionLevelUpEvent : GameEvent
{
    public ProfessionType profession;
    public int oldLevel;
    public int newLevel;
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

// ==================== Cooking Events ====================

public class CraftingStartedEvent : GameEvent
{
    public RecipeData recipe;
    public int quantity;
    public int currentIndex; // Which item in the queue (1-based)
}

public class CraftingProgressEvent : GameEvent
{
    public float progress; // 0 to 1
    public RecipeData recipe;
    public int currentIndex;
    public int totalQuantity;
}

public class CraftingCompletedEvent : GameEvent
{
    public RecipeData recipe;
    public ItemData craftedItem;
    public int quantity;
    public int xpGained;
}

public class CraftingCancelledEvent : GameEvent
{
    public RecipeData recipe;
    public int itemsCrafted;
    public int itemsRemaining;
}

public class RecipeUnlockedEvent : GameEvent
{
    public RecipeData recipe;
}

// ==================== Message Events ====================

public enum GameMessageType
{
    Info,
    Success,
    Warning,
    Error
}

public class MessageEvent : GameEvent
{
    public string message;
    public GameMessageType messageType;
}

// ==================== Skill Events ====================

public class SkillCastEvent : GameEvent
{
    public SkillData skill;
    public bool isPlayerCast;
    public int casterIndex; // For monsters, -1 for player
}

public class SkillHitEvent : GameEvent
{
    public SkillData skill;
    public float damage;
    public bool wasCritical;
    public int targetIndex; // -1 for player
}

public class BuffAppliedEvent : GameEvent
{
    public SkillData sourceSkill;
    public float duration;
    public bool isOnPlayer;
    public int targetIndex; // For monsters, -1 for player
}

public class BuffExpiredEvent : GameEvent
{
    public SkillData sourceSkill;
    public bool wasOnPlayer;
    public int targetIndex;
}

public class DebuffAppliedEvent : GameEvent
{
    public SkillData sourceSkill;
    public float duration;
    public bool isOnPlayer;
    public int targetIndex;
}

public class DebuffExpiredEvent : GameEvent
{
    public SkillData sourceSkill;
    public bool wasOnPlayer;
    public int targetIndex;
}

public class ActionBarChangedEvent : GameEvent
{
    public int slotIndex;
    public SkillData skill; // null if slot cleared
}

public class SkillCooldownUpdatedEvent : GameEvent
{
    public SkillData skill;
    public float remainingCooldown;
    public float totalCooldown;
}

