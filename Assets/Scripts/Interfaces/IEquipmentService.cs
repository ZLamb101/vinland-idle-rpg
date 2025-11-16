using System;
using System.Collections.Generic;

/// <summary>
/// Interface for equipment management services
/// </summary>
public interface IEquipmentService
{
    // Events migrated to EventBus - see GameEvent.cs
    
    // Equipment Management
    bool EquipItem(EquipmentData equipment);
    EquipmentData UnequipItem(EquipmentSlot slot);
    EquipmentData GetEquipment(EquipmentSlot slot);
    bool IsSlotEmpty(EquipmentSlot slot);
    Dictionary<EquipmentSlot, EquipmentData> GetAllEquippedItems();
    
    // Stats
    float GetTotalAttackDamage();
    float GetTotalAttackSpeed();
    float GetTotalMaxHealth();
    float GetTotalHealthRegen();
    float GetTotalArmor();
    float GetTotalDodge();
    float GetTotalCriticalChance();
    float GetTotalLifesteal();
    float GetTotalXPBonus();
    float GetTotalGoldBonus();
    EquipmentStats GetTotalStats();
    
    // Save/Load
    Dictionary<EquipmentSlot, string> GetEquipmentSaveData();
    void LoadEquipmentData(Dictionary<EquipmentSlot, string> saveData);
    void ClearAllEquipment();
}

