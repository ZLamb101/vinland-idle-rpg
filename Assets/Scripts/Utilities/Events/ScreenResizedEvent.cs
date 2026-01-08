using UnityEngine;

/// <summary>
/// Event fired when the screen resolution changes
/// </summary>
public class ScreenResizedEvent
{
    public int oldWidth;
    public int oldHeight;
    public int newWidth;
    public int newHeight;
    
    public ScreenResizedEvent(int oldWidth, int oldHeight, int newWidth, int newHeight)
    {
        this.oldWidth = oldWidth;
        this.oldHeight = oldHeight;
        this.newWidth = newWidth;
        this.newHeight = newHeight;
    }
}


