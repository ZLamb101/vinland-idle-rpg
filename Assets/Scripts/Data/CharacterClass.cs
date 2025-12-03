/// <summary>
/// Character class enum with flags support for multi-class skills.
/// Use flags to allow skills to be available to multiple classes.
/// </summary>
[System.Flags]
public enum CharacterClass
{
    None = 0,
    Warrior = 1 << 0,   // 1
    Hunter = 1 << 1,    // 2
    Mage = 1 << 2,      // 4
    Rogue = 1 << 3,     // 8
    Priest = 1 << 4,    // 16
    Paladin = 1 << 5,   // 32
    All = Warrior | Hunter | Mage | Rogue | Priest | Paladin
}

/// <summary>
/// Helper methods for CharacterClass enum
/// </summary>
public static class CharacterClassHelper
{
    /// <summary>
    /// Parse a string class name to CharacterClass enum.
    /// Used for compatibility with existing string-based class storage.
    /// </summary>
    public static CharacterClass ParseFromString(string className)
    {
        if (string.IsNullOrEmpty(className))
            return CharacterClass.None;
        
        // Case-insensitive matching
        switch (className.ToLower())
        {
            case "warrior":
                return CharacterClass.Warrior;
            case "hunter":
                return CharacterClass.Hunter;
            case "mage":
                return CharacterClass.Mage;
            case "rogue":
                return CharacterClass.Rogue;
            case "priest":
                return CharacterClass.Priest;
            case "paladin":
                return CharacterClass.Paladin;
            default:
                UnityEngine.Debug.LogWarning($"[CharacterClass] Unknown class name: {className}");
                return CharacterClass.None;
        }
    }
    
    /// <summary>
    /// Check if a character class is allowed by the given flags.
    /// </summary>
    public static bool IsClassAllowed(CharacterClass allowedClasses, CharacterClass playerClass)
    {
        // If All is set, allow everyone
        if (allowedClasses == CharacterClass.All)
            return true;
        
        // Check if the player's class flag is set in allowedClasses
        return (allowedClasses & playerClass) != 0;
    }
}

