using Core.Attributes;
using DNExtensions;
using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "New Radio Message", menuName = "Scriptable Objects/New Radio Message")]
public class SORadioMessage : ScriptableObject
{

    [Header("Message Info")]
    [SerializeField, CreateEditableAsset] private SOCharacter sender;
    [SerializeField] private SOAudioEvent audioEvent;
    [Tooltip("If true, this message will skip to the start of the radios message queue")]
    [SerializeField] private bool isImportant;
    [Tooltip("If true, this message will stay on screen until replaced by another persistent message")]
    [SerializeField] private bool isPersistent;
    [Tooltip("For persistent messages: minimum show time before it can be replaced\nFor non-persistent messages: total show time before auto-hide")]
    [SerializeField, Min(2)] private float duration = 3f;
    [SerializeField, TextArea(1,5)] private string message;



    [Button]
    public void PlayMessage()
    {
        RadioManager.Instance?.AddMessage(this);
    }
    
    
    
    public bool IsImportant => isImportant;
    public bool IsPersistent => isPersistent;
    /// <summary>
    /// For persistent messages: minimum show time before it can be replaced
    /// For non-persistent messages: total show time before auto-hide
    /// </summary>
    public float Duration => duration;
    public SOCharacter Sender => sender;
    public string Message => message;
    public SOAudioEvent AudioEvent => audioEvent;
}
