using UnityEngine;
using TMPro;
using VInspector;

public class ReplaceTextWithBinding : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private bool autoSetOnStart = true;
    [SerializeField] private bool useSpriteMode = true;

    private void Start()
    {
        if (autoSetOnStart)
        {
            UpdateBindingText();
        }
    }

    [Button]
    public void UpdateBindingText()
    {
        if (!textComponent) return;

        string processedText = useSpriteMode 
            ? InputManager.ReplaceActionBindingsWithSprites(textComponent.text)
            : InputManager.ReplaceActionBindingsWithText(textComponent.text);
            
        textComponent.text = processedText;
    }
}