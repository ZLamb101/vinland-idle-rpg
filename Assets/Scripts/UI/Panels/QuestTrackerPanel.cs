using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class QuestTrackerPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject questEntryPrefab; // Prefab with TextMeshProUGUI
    public Transform contentContainer; // VerticalLayoutGroup
    
    private Dictionary<string, GameObject> questEntries = new Dictionary<string, GameObject>();
    
    void Start()
    {
        // Wait for QuestManager to initialize, then refresh
        StartCoroutine(InitializeWhenReady());
        
        // Subscribe to events
        EventBus.Subscribe<QuestAcceptedEvent>(OnQuestAccepted);
        EventBus.Subscribe<QuestObjectiveUpdatedEvent>(OnQuestUpdated);
        EventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
        EventBus.Subscribe<QuestTurnedInEvent>(OnQuestTurnedIn);
        EventBus.Subscribe<CharacterLoadedEvent>(OnCharacterLoaded);
    }
    
    System.Collections.IEnumerator InitializeWhenReady()
    {
        // Wait until QuestManager is registered
        while (!Services.IsRegistered<IQuestService>())
        {
            yield return null;
        }
        
        // Now refresh
        RefreshAllQuests();
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<QuestAcceptedEvent>(OnQuestAccepted);
        EventBus.Unsubscribe<QuestObjectiveUpdatedEvent>(OnQuestUpdated);
        EventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
        EventBus.Unsubscribe<QuestTurnedInEvent>(OnQuestTurnedIn);
        EventBus.Unsubscribe<CharacterLoadedEvent>(OnCharacterLoaded);
    }
    
    void OnQuestAccepted(QuestAcceptedEvent e) => RefreshAllQuests();
    void OnQuestUpdated(QuestObjectiveUpdatedEvent e) => RefreshQuest(e.quest);
    void OnQuestCompleted(QuestCompletedEvent e) => RefreshQuest(e.quest);
    void OnQuestTurnedIn(QuestTurnedInEvent e) => RemoveQuest(e.quest.id);
    void OnCharacterLoaded(CharacterLoadedEvent e) => RefreshAllQuests();
    
    void RefreshAllQuests()
    {
        foreach (var entry in questEntries.Values)
        {
            Destroy(entry);
        }
        questEntries.Clear();
        
        if (!Services.TryGet<IQuestService>(out IQuestService questService))
        {
            return;
        }
        
        List<PlayerQuest> activeQuests = questService.GetActiveQuests();
        if (activeQuests == null || activeQuests.Count == 0)
        {
            return;
        }
        
        foreach (var pQuest in activeQuests)
        {
            QuestData questData = questService.GetQuestData(pQuest.questId);
            
            if (questData != null)
            {
                CreateOrUpdateEntry(questData, pQuest);
            }
            else
            {
                Debug.LogWarning($"[QuestTrackerPanel] Could not find QuestData for quest ID: {pQuest.questId}");
            }
        }
    }
    
    void RefreshQuest(QuestData quest)
    {
        if (!Services.TryGet<IQuestService>(out IQuestService questService))
            return;
        
        List<PlayerQuest> activeQuests = questService.GetActiveQuests();
        PlayerQuest pQuest = activeQuests.Find(q => q.questId == quest.id);
        
        if (pQuest != null)
        {
            CreateOrUpdateEntry(quest, pQuest);
        }
    }
    
    void RemoveQuest(string questId)
    {
        if (questEntries.TryGetValue(questId, out GameObject entry))
        {
            Destroy(entry);
            questEntries.Remove(questId);
        }
    }
    
    void CreateOrUpdateEntry(QuestData quest, PlayerQuest pQuest)
    {
        if (!questEntries.TryGetValue(quest.id, out GameObject entry))
        {
            if (questEntryPrefab == null)
            {
                Debug.LogError("[QuestTrackerPanel] questEntryPrefab is null! Please assign it in the Inspector.");
                return;
            }
            
            if (contentContainer == null)
            {
                Debug.LogError("[QuestTrackerPanel] contentContainer is null! Please assign it in the Inspector.");
                return;
            }
            
            entry = Instantiate(questEntryPrefab, contentContainer);
            entry.SetActive(true);
            questEntries.Add(quest.id, entry);
        }
        
        TextMeshProUGUI textComp = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
        {
            textComp.text = BuildQuestText(quest, pQuest);
        }
        else
        {
            Debug.LogError($"[QuestTrackerPanel] No TextMeshProUGUI component found in quest entry prefab for: {quest.questName}. Check the prefab structure.");
        }
    }
    
    string BuildQuestText(QuestData quest, PlayerQuest pQuest)
    {
        StringBuilder sb = new StringBuilder();
        
        sb.Append($"<b>{quest.questName}</b>");
        if (pQuest.state == QuestState.CanTurnIn)
        {
            sb.Append(" <color=green>(Complete)</color>");
        }
        sb.AppendLine();
        
        for (int i = 0; i < quest.objectives.Count; i++)
        {
            var obj = quest.objectives[i];
            int progress = (i < pQuest.objectiveProgress.Count) ? pQuest.objectiveProgress[i] : 0;
            
            string desc = obj.description;
            if (string.IsNullOrEmpty(desc))
            {
                desc = $"{obj.type} {obj.targetID}";
            }
            
            string color = (progress >= obj.amount) ? "grey" : "white";
            string check = (progress >= obj.amount) ? "✓" : "-";
            
            sb.AppendLine($"<color={color}>{check} {desc} ({progress}/{obj.amount})</color>");
        }
        
        return sb.ToString();
    }
}

