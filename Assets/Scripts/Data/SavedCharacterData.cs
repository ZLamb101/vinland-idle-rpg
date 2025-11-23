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
    
    // Character attributes
    public int strength = 10;
    public int agility = 10;
    public int intellect = 10;
    public int stamina = 10;
    public int spirit = 10;
    
    // Metadata
    public DateTime createdDate;
    public DateTime lastPlayedDate;
    public bool isEmpty = true; // Is this slot empty?
    
    // Away Activity (for display)
    public int awayActivityType = 0;
    public string awayActivityDisplay = "";
    
    // Convert from CharacterData
    public void SaveFrom(CharacterData data, string race, string charClass)
    {
        characterName = data.characterName;
        level = data.level;
        currentXP = data.currentXP;
        gold = data.gold;
        currentHealth = data.currentHealth;
        inventory = data.inventory;
        // Use race/class from CharacterData if available, otherwise use parameters
        this.race = !string.IsNullOrEmpty(data.race) ? data.race : race;
        this.characterClass = !string.IsNullOrEmpty(data.characterClass) ? data.characterClass : charClass;
        // Save character attributes
        this.strength = data.strength;
        this.agility = data.agility;
        this.intellect = data.intellect;
        this.stamina = data.stamina;
        this.spirit = data.spirit;
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
        data.race = race; // Load race
        data.characterClass = characterClass; // Load class
        // Load character attributes
        data.strength = strength;
        data.agility = agility;
        data.intellect = intellect;
        data.stamina = stamina;
        data.spirit = spirit;
    }
    
    // Get display string for slot (e.g., "Level 5 Troll Hunter")
    public string GetDescription()
    {
        if (isEmpty) return "Empty Slot";
        return $"Level {level} {race} {characterClass}";
    }
}

