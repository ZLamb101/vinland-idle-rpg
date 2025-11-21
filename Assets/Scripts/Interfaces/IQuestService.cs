using System.Collections.Generic;

public interface IQuestService
{
    void ShowQuestOffer(QuestData quest, NPCData npc);
    void ShowQuestTurnIn(QuestData quest, NPCData npc);
    void AcceptQuest(QuestData quest, NPCData npc = null);
    void TurnInQuest(QuestData quest, NPCData npc = null);
    void AbandonQuest(QuestData quest);
    
    bool CanAcceptQuest(QuestData quest);
    bool CanTurnInQuest(QuestData quest);
    bool IsQuestActive(QuestData quest);
    bool IsQuestCompleted(QuestData quest);
    
    QuestState GetQuestState(QuestData quest);
    List<PlayerQuest> GetActiveQuests();
    QuestData GetQuestData(string questId);
}
