using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour, IQuestService
{
    [Header("Quest Database")]
    [Tooltip("Optional: Assign all QuestData manually. If empty, will load from Resources folder.")]
    public List<QuestData> questDatabase = new List<QuestData>();
    
    private ICharacterService characterService;
    private IGameLogService gameLogService;
    
    private Dictionary<string, QuestData> questCache;

    void Awake()
    {
        // Singleton pattern - destroy duplicate instances
        if (Services.IsRegistered<IQuestService>())
        {
            Debug.LogWarning("[QuestManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        Services.Register<IQuestService>(this);
        
        RefreshQuestCache();
    }

    void Start()
    {
        EnsureServicesInitialized();
        
        EventBus.Subscribe<MonsterDiedEvent>(OnMonsterDied);
        EventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
    }
    
    private void EnsureServicesInitialized()
    {
        if (characterService == null)
        {
            characterService = Services.Get<ICharacterService>();
        }
        
        if (gameLogService == null)
        {
            Services.TryGet<IGameLogService>(out gameLogService);
        }
    }
    
    private void RefreshQuestCache()
    {
        questCache = new Dictionary<string, QuestData>();
        
        if (questDatabase != null && questDatabase.Count > 0)
        {
            foreach (var q in questDatabase)
            {
                if (q != null && !string.IsNullOrEmpty(q.id) && !questCache.ContainsKey(q.id))
                {
                    questCache.Add(q.id, q);
                }
            }
        }
        else
        {
            QuestData[] quests = Resources.LoadAll<QuestData>("");
            foreach (var q in quests)
            {
                if (q != null && !string.IsNullOrEmpty(q.id) && !questCache.ContainsKey(q.id))
                {
                    questCache.Add(q.id, q);
                }
            }
        }
        
        Debug.Log($"[QuestManager] Loaded {questCache.Count} quests");
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<MonsterDiedEvent>(OnMonsterDied);
        EventBus.Unsubscribe<ItemAddedEvent>(OnItemAdded);
        Services.Unregister<IQuestService>();
    }

    public void ShowQuestOffer(QuestData quest, NPCData npc)
    {
        EnsureServicesInitialized();
        if (!CanAcceptQuest(quest)) return;
        
        if (npc != null && Services.TryGet<IDialogueService>(out var dialogueService))
        {
            List<string> dialogueLines = new List<string>();
            
            // Use custom dialogue pages if available, otherwise use description
            if (quest.dialoguePages != null && quest.dialoguePages.Count > 0)
            {
                dialogueLines.AddRange(quest.dialoguePages);
            }
            else
            {
                dialogueLines.Add($"{npc.npcName} has a task for you:\n\n{quest.description}");
            }
            
            // Add objectives and rewards as the final page
            string objectivesText = BuildQuestObjectivesText(quest);
            string rewardsText = BuildQuestRewardsText(quest);
            dialogueLines.Add($"{objectivesText}\n\n{rewardsText}");
            
            dialogueService.StartCustomDialogueWithActions(
                npc, 
                dialogueLines.ToArray(), 
                () => AcceptQuest(quest, npc),
                null
            );
        }
    }

    public void AcceptQuest(QuestData quest, NPCData npc = null)
    {
        EnsureServicesInitialized();
        if (!CanAcceptQuest(quest)) return;

        CharacterData charData = characterService.GetCharacterData();
        PlayerQuest newQuest = new PlayerQuest(quest.id, quest.objectives.Count);
        charData.activeQuests.Add(newQuest);

        EventBus.Publish(new QuestAcceptedEvent { quest = quest });
        
        if (gameLogService != null)
            gameLogService.AddLogEntry($"Accepted quest: {quest.questName}", LogType.Info);

        CheckObjectives(newQuest, quest);
    }

    public void ShowQuestTurnIn(QuestData quest, NPCData npc)
    {
        EnsureServicesInitialized();
        if (!CanTurnInQuest(quest)) return;
        
        if (npc != null && Services.TryGet<IDialogueService>(out var dialogueService))
        {
            string objectivesText = BuildQuestObjectivesText(quest);
            string rewardsText = BuildQuestRewardsText(quest);
            
            string[] dialogueLines = new string[]
            {
                $"Excellent work! You've completed the task:\n\n{objectivesText}",
                $"Here is your reward:\n\n{rewardsText}"
            };
            
            dialogueService.StartCustomDialogueWithActions(
                npc, 
                dialogueLines, 
                () => TurnInQuest(quest, npc),
                null,
                "Turn In"
            );
        }
    }

    public void TurnInQuest(QuestData quest, NPCData npc = null)
    {
        EnsureServicesInitialized();
        if (!CanTurnInQuest(quest)) return;

        CharacterData charData = characterService.GetCharacterData();
        PlayerQuest playerQuest = charData.activeQuests.Find(q => q.questId == quest.id);

        ProcessQuestRewards(quest);
        ConsumeCollectObjectives(quest);
        
        charData.activeQuests.Remove(playerQuest);
        if (!charData.completedQuestIDs.Contains(quest.id))
        {
            charData.completedQuestIDs.Add(quest.id);
        }

        EventBus.Publish(new QuestTurnedInEvent 
        { 
            quest = quest, 
            xpReward = quest.xpReward, 
            goldReward = quest.goldReward, 
            itemReward = quest.itemReward 
        });
        
        if (gameLogService != null)
             gameLogService.AddLogEntry($"Completed quest: {quest.questName}", LogType.Success);
        
        if (Services.TryGet<IDialogueService>(out var dialogueService))
        {
            dialogueService.EndDialogue();
        }
    }

    private void ProcessQuestRewards(QuestData quest)
    {
        characterService.AddXP(quest.xpReward);
        characterService.AddGold(quest.goldReward);

        if (quest.itemReward != null)
        {
            InventoryItem rewardItem = quest.itemReward.CreateInventoryItem(quest.itemRewardQuantity);
            if (!characterService.AddItemToInventory(rewardItem) && gameLogService != null)
            {
                gameLogService.AddLogEntry("Inventory full! Reward item lost.", LogType.Error);
            }
        }
    }

    private void ConsumeCollectObjectives(QuestData quest)
    {
        foreach (var obj in quest.objectives)
        {
            if (obj.type == QuestObjectiveType.Collect)
            {
                RemoveQuestItems(obj.targetID, obj.amount);
            }
        }
    }

    public void AbandonQuest(QuestData quest)
    {
        EnsureServicesInitialized();
        CharacterData charData = characterService.GetCharacterData();
        PlayerQuest playerQuest = charData.activeQuests.Find(q => q.questId == quest.id);
        
        if (playerQuest != null)
        {
            charData.activeQuests.Remove(playerQuest);
            if (gameLogService != null)
                gameLogService.AddLogEntry($"Abandoned quest: {quest.questName}", LogType.Warning);
        }
    }

    public bool CanAcceptQuest(QuestData quest)
    {
        EnsureServicesInitialized();
        if (quest == null) return false;
        
        CharacterData charData = characterService.GetCharacterData();
        
        if (charData.activeQuests.Exists(q => q.questId == quest.id)) return false;
        if (charData.completedQuestIDs.Contains(quest.id)) return false;
        
        if (quest.prerequisiteQuest != null && 
            !charData.completedQuestIDs.Contains(quest.prerequisiteQuest.id))
        {
            return false;
        }
        
        if (charData.level < quest.levelRequired) return false;

        return true;
    }

    public bool CanTurnInQuest(QuestData quest)
    {
        PlayerQuest playerQuest = GetPlayerQuest(quest);
        if (playerQuest == null) return false;
        
        return playerQuest.state == QuestState.CanTurnIn;
    }

    public bool IsQuestActive(QuestData quest)
    {
        return GetPlayerQuest(quest) != null;
    }

    public bool IsQuestCompleted(QuestData quest)
    {
        EnsureServicesInitialized();
        CharacterData charData = characterService.GetCharacterData();
        return charData.completedQuestIDs.Contains(quest.id);
    }

    public QuestState GetQuestState(QuestData quest)
    {
        if (IsQuestCompleted(quest)) return QuestState.Completed;
        PlayerQuest playerQuest = GetPlayerQuest(quest);
        return playerQuest != null ? playerQuest.state : QuestState.NotStarted;
    }

    public List<PlayerQuest> GetActiveQuests()
    {
        EnsureServicesInitialized();
        if (characterService == null)
        {
            Debug.LogWarning("[QuestManager] CharacterService not available yet");
            return new List<PlayerQuest>();
        }
        return characterService.GetCharacterData().activeQuests;
    }
    
    public QuestData GetQuestData(string questId)
    {
        if (questCache != null && questCache.TryGetValue(questId, out QuestData data))
        {
            return data;
        }
        return null;
    }
    
    private PlayerQuest GetPlayerQuest(QuestData quest)
    {
        EnsureServicesInitialized();
        if (characterService == null) return null;
        
        CharacterData charData = characterService.GetCharacterData();
        return charData.activeQuests.Find(q => q.questId == quest.id);
    }

    private void OnMonsterDied(MonsterDiedEvent e)
    {
        UpdateObjectives(QuestObjectiveType.Kill, e.monsterData.monsterName, 1);
    }

    private void OnItemAdded(ItemAddedEvent e)
    {
        if (e.wasSuccessful)
        {
            UpdateObjectives(QuestObjectiveType.Collect, e.item.itemName, e.quantity);
        }
    }

    private void UpdateObjectives(QuestObjectiveType type, string targetID, int amount)
    {
        EnsureServicesInitialized();
        if (characterService == null) return;
        
        CharacterData charData = characterService.GetCharacterData();
        List<PlayerQuest> activeQuests = charData.activeQuests;
        
        for (int i = 0; i < activeQuests.Count; i++)
        {
            PlayerQuest pQuest = activeQuests[i];
            QuestData questData = GetQuestData(pQuest.questId);
            
            if (questData == null) continue;
            
            bool questUpdated = false;
            bool questCompleted = true;

            for (int j = 0; j < questData.objectives.Count; j++)
            {
                QuestObjective obj = questData.objectives[j];
                if (obj.type == type && obj.targetID == targetID)
                {
                    int currentProgress = pQuest.objectiveProgress[j];
                    if (currentProgress < obj.amount)
                    {
                        int newProgress = Mathf.Min(currentProgress + amount, obj.amount);
                        if (newProgress != currentProgress)
                        {
                            pQuest.objectiveProgress[j] = newProgress;
                            questUpdated = true;
                            
                            EventBus.Publish(new QuestObjectiveUpdatedEvent 
                            { 
                                quest = questData, 
                                objectiveIndex = j, 
                                currentAmount = newProgress, 
                                targetAmount = obj.amount 
                            });
                        }
                    }
                }
                
                if (pQuest.objectiveProgress[j] < obj.amount)
                {
                    questCompleted = false;
                }
            }
            
            if (questUpdated)
            {
                if (questCompleted && pQuest.state != QuestState.CanTurnIn)
                {
                    pQuest.state = QuestState.CanTurnIn;
                    EventBus.Publish(new QuestCompletedEvent { quest = questData });
                    
                    if (gameLogService != null)
                    {
                        string turnInTarget = questData.turnInNPC != null 
                            ? $"Return to {questData.turnInNPC.npcName}." 
                            : "Return to the quest giver.";
                        gameLogService.AddLogEntry($"Quest objectives complete: {questData.questName}. {turnInTarget}", LogType.Success);
                    }
                }
            }
        }
    }

    private void CheckObjectives(PlayerQuest pQuest, QuestData questData)
    {
        EnsureServicesInitialized();
        if (characterService == null) return;
        
        bool allCompleted = true;
        bool updated = false;
        CharacterData charData = characterService.GetCharacterData();
        
        for (int i = 0; i < questData.objectives.Count; i++)
        {
            QuestObjective obj = questData.objectives[i];
            if (obj.type == QuestObjectiveType.Collect)
            {
                int count = CountItemInInventory(charData.inventory, obj.targetID);
                int newProgress = Mathf.Min(count, obj.amount);
                
                if (pQuest.objectiveProgress[i] != newProgress)
                {
                    pQuest.objectiveProgress[i] = newProgress;
                    updated = true;
                }
            }
            
            if (pQuest.objectiveProgress[i] < obj.amount)
            {
                allCompleted = false;
            }
        }
        
        if (updated && allCompleted)
        {
            pQuest.state = QuestState.CanTurnIn;
            EventBus.Publish(new QuestCompletedEvent { quest = questData });
        }
    }
    
    private int CountItemInInventory(InventoryData inventory, string itemName)
    {
        int count = 0;
        foreach (var item in inventory.items)
        {
            if (item != null && !item.IsEmpty() && item.itemName == itemName)
            {
                count += item.quantity;
            }
        }
        return count;
    }
    
    private void RemoveQuestItems(string itemName, int amountToRemove)
    {
        EnsureServicesInitialized();
        if (characterService == null) return;
        
        InventoryData inventory = characterService.GetCharacterData().inventory;
        int remaining = amountToRemove;
        
        for (int i = 0; i < inventory.items.Length; i++)
        {
            if (remaining <= 0) break;
            
            InventoryItem item = inventory.items[i];
            if (item != null && !item.IsEmpty() && item.itemName == itemName)
            {
                int take = Mathf.Min(remaining, item.quantity);
                characterService.RemoveItemFromInventory(i, take);
                remaining -= take;
            }
        }
    }
    
    private string BuildQuestObjectivesText(QuestData quest)
    {
        if (quest.objectives == null || quest.objectives.Count == 0)
        {
            return "Complete the task.";
        }
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Objectives:");
        
        foreach (var obj in quest.objectives)
        {
            string desc = obj.description;
            if (string.IsNullOrEmpty(desc))
            {
                desc = $"{obj.type} {obj.amount} {obj.targetID}";
            }
            sb.AppendLine($"• {desc}");
        }
        
        return sb.ToString().TrimEnd();
    }
    
    private string BuildQuestRewardsText(QuestData quest)
    {
        List<string> rewards = new List<string>();
        
        if (quest.xpReward > 0)
        {
            rewards.Add($"{quest.xpReward} XP");
        }
        
        if (quest.goldReward > 0)
        {
            rewards.Add($"{quest.goldReward} Gold");
        }
        
        if (quest.itemReward != null)
        {
            string itemName = quest.itemReward.itemName;
            int quantity = quest.itemRewardQuantity;
            if (quantity > 1)
            {
                rewards.Add($"{quantity}x {itemName}");
            }
            else
            {
                rewards.Add(itemName);
            }
        }
        
        if (rewards.Count == 0)
        {
            return "Rewards: None";
        }
        
        return "Rewards: " + string.Join(", ", rewards);
    }
}
