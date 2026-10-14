using UnityEngine;

// Optional development display; no policy, storage, or input handling.
public sealed class MonsterLearningView : MonoBehaviour
{
    private string displayText;
    private bool visible;
    public void Render(bool show, string text) { visible = show; displayText = text; }
    private void OnGUI()
    {
        if (visible) GUI.Box(new Rect(12, 12, 500, 145), displayText);
    }
}
