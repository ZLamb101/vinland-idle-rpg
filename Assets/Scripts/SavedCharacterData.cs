using System;
using UnityEngine;

/// <summary>
/// Serializable data for a saved character.
/// Used for character selection screen.
/// </summary>
[System.Serializable]
public class SavedCharacterData
{
    public string characterName = "";
    public int level = 1;
    public int currentXP = 0;
    public int gold = 0;
    public float currentHealth = 50f;
    public InventoryData inventory = new InventoryData();
    
    // Character creation data
    public string race = "Human";
    public string characterClass = "Warrior";
    
    // Metadata
    public DateTime createdDate;
    public DateTime lastPlayedDate;
    public bool isEmpty = true; // Is this slot empty?
    
    // Convert from CharacterData
    public void SaveFrom(CharacterData data, string race, string charClass)
    {
        characterName = data.characterName;
        level = data.level;
        currentXP = data.currentXP;
        gold = data.gold;
        currentHealth = data.currentHealth;
        inventory = data.inventory;
        this.race = race;
        this.characterClass = charClass;
        lastPlayedDate = DateTime.Now;
        isEmpty = false;
    }
    
    // Load into CharacterData
    public void LoadInto(CharacterData data)
    {
        data.characterName = characterName;
        data.level = level;
        data.currentXP = currentXP;
        data.gold = gold;
        data.currentHealth = currentHealth;
        data.inventory = inventory;
    }
    
    // Get display string for slot (e.g., "Level 5 Troll Hunter")
    public string GetDescription()
    {
        if (isEmpty) return "Empty Slot";
        return $"Level {level} {race} {characterClass}";
    }
}

