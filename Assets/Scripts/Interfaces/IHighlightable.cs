/// <summary>
/// Implemented by interactables that support a visual highlight when targeted by the player.
/// </summary>
public interface IHighlightable
{
    /// <summary>
    /// Enable or disable the highlight visual on this object.
    /// </summary>
    void SetHighlight(bool highlighted);
}
