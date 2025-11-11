using UnityEngine;
using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Debug utilities for the save system
/// Provides menu commands to open save folder and view save files
/// </summary>
public static class SaveSystemDebug
{
    /// <summary>
    /// Open the save folder in Windows Explorer / Finder
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AddDebugKeyListener()
    {
        // You can press Ctrl+Shift+S in-game to open save folder
        if (Application.isPlaying)
        {
            GameObject debugObj = new GameObject("SaveSystemDebug");
            debugObj.AddComponent<SaveSystemDebugComponent>();
            Object.DontDestroyOnLoad(debugObj);
        }
    }
    
    /// <summary>
    /// Open the save folder in file explorer
    /// </summary>
    public static void OpenSaveFolder()
    {
        string savePath = SaveSystem.GetSaveFolderPath();
        
        UnityEngine.Debug.Log($"[SaveSystemDebug] Opening save folder: {savePath}");
        
        // Open in file explorer (cross-platform)
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        Process.Start("explorer.exe", savePath);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        Process.Start("open", savePath);
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        Process.Start("xdg-open", savePath);
#else
        UnityEngine.Debug.Log($"Save folder path: {savePath}");
#endif
    }
    
    /// <summary>
    /// Print all save file information to console
    /// </summary>
    public static void PrintAllSaveFiles()
    {
        UnityEngine.Debug.Log("=== Save System Information ===");
        UnityEngine.Debug.Log($"Save Folder: {SaveSystem.GetSaveFolderPath()}");
        UnityEngine.Debug.Log($"");
        
        int[] savedSlots = SaveSystem.GetSavedCharacterSlots();
        
        if (savedSlots.Length == 0)
        {
            UnityEngine.Debug.Log("No save files found.");
            return;
        }
        
        UnityEngine.Debug.Log($"Found {savedSlots.Length} saved character(s):");
        
        foreach (int slot in savedSlots)
        {
            SaveFileInfo info = SaveSystem.GetSaveFileInfo(slot);
            if (info != null)
            {
                UnityEngine.Debug.Log($"  Slot {slot}: {info.characterName} (Level {info.level} {info.race} {info.characterClass})");
                UnityEngine.Debug.Log($"    File: {SaveSystem.GetSaveFilePath(slot)}");
                UnityEngine.Debug.Log($"    Last Saved: {info.saveTime}");
            }
        }
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// Unity Editor menu item to open save folder
    /// </summary>
    [MenuItem("Tools/Save System/Open Save Folder")]
    public static void EditorOpenSaveFolder()
    {
        OpenSaveFolder();
    }
    
    /// <summary>
    /// Unity Editor menu item to print save file info
    /// </summary>
    [MenuItem("Tools/Save System/Print Save File Info")]
    public static void EditorPrintSaveFiles()
    {
        PrintAllSaveFiles();
    }
    
    /// <summary>
    /// Unity Editor menu item to clear all saves (WARNING!)
    /// </summary>
    [MenuItem("Tools/Save System/Clear All Saves (WARNING)")]
    public static void EditorClearAllSaves()
    {
        if (EditorUtility.DisplayDialog("Clear All Saves", 
            "Are you sure you want to DELETE ALL save files? This cannot be undone!", 
            "Delete All", "Cancel"))
        {
            int[] slots = SaveSystem.GetSavedCharacterSlots();
            foreach (int slot in slots)
            {
                SaveSystem.DeleteCharacter(slot);
            }
            UnityEngine.Debug.Log($"[SaveSystemDebug] Deleted {slots.Length} save file(s)");
        }
    }
#endif
}

/// <summary>
/// MonoBehaviour component to listen for hotkey in-game
/// </summary>
public class SaveSystemDebugComponent : MonoBehaviour
{
    void Update()
    {
        // Check if new Input System is available
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            
            // Press Ctrl+Shift+S to open save folder
            if ((keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) && 
                (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) && 
                keyboard.sKey.wasPressedThisFrame)
            {
                SaveSystemDebug.OpenSaveFolder();
            }
            
            // Press Ctrl+Shift+I to print save info
            if ((keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) && 
                (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) && 
                keyboard.iKey.wasPressedThisFrame)
            {
                SaveSystemDebug.PrintAllSaveFiles();
            }
        }
        #else
        // Fallback to legacy Input system
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.S))
        {
            SaveSystemDebug.OpenSaveFolder();
        }
        
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.I))
        {
            SaveSystemDebug.PrintAllSaveFiles();
        }
        #endif
    }
}

