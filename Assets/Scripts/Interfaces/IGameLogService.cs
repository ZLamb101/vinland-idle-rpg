/// <summary>
/// Interface for game log services.
/// Handles displaying game log entries and combat log entries.
/// </summary>
public interface IGameLogService
{
    // Log Entry Methods
    void AddLogEntry(string message, LogType logType = LogType.Info);
    void AddCombatLogEntry(string message, LogType logType = LogType.Info);
    
    // Log Visibility Control
    void ToggleLog();
    void ShowLog();
    void HideLog();
    void ClearLog();
    
    // Tab Control
    void SwitchToTab(bool showCombatLog);
    
    // Viewport Control
    void ToggleViewport();
    void ShowViewport();
    void HideViewport();
}

