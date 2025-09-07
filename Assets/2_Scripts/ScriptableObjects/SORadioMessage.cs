using DNExtensions;
using UnityEngine;

[CreateAssetMenu(fileName = "New Radio Message", menuName = "Scriptable Objects/New Radio Message")]
public class SORadioMessage : ScriptableObject
{

    [Header("Radio Message Info")]
    [SerializeField] private SOCharacter sender;
    [SerializeField] private SOAudioEvent audioEvent;
    [SerializeField] private bool isImportant;
    [SerializeField, Min(2)] private float duration = 3f;
    [SerializeField, TextArea(1,5)] private string message;

    

    public float Duration => duration;
    public bool IsImportant => isImportant;
    public SOCharacter Sender => sender;
    public string Message => message;
    public SOAudioEvent AudioEvent => audioEvent;
}
