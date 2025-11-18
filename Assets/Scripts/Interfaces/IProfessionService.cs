/// <summary>
/// Service interface for managing character professions
/// </summary>
public interface IProfessionService
{
    /// <summary>
    /// Add experience to a profession and handle level-ups
    /// </summary>
    void AddProfessionXP(ProfessionType profession, int amount);
    
    /// <summary>
    /// Get the current level of a profession
    /// </summary>
    int GetProfessionLevel(ProfessionType profession);
    
    /// <summary>
    /// Get the current XP of a profession
    /// </summary>
    int GetProfessionXP(ProfessionType profession);
    
    /// <summary>
    /// Get XP required for next level
    /// </summary>
    int GetProfessionXPRequired(ProfessionType profession);
    
    /// <summary>
    /// Get the profession data (for saving/loading)
    /// </summary>
    ProfessionData GetProfessionData();
    
    /// <summary>
    /// Load profession data (from save system)
    /// </summary>
    void LoadProfessionData(ProfessionData data);
}


