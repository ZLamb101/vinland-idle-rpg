using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestState
{
    NotStarted,
    InProgress,
    CanTurnIn,
    Completed
}

[Serializable]
public class PlayerQuest
{
    public string questId;
    public QuestState state;
    public List<int> objectiveProgress;
    
    public PlayerQuest(string id, int objectiveCount)
    {
        questId = id;
        state = QuestState.InProgress;
        objectiveProgress = new List<int>(new int[objectiveCount]);
    }
}



