using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Scriptable Objects/New Character")]
public class SOCharacter : ScriptableObject
{

    [Header("Character Info")]
    [SerializeField] private new string name;
    [SerializeField] private string title;
    [SerializeField, TextArea(1,3)] private string description;
    [SerializeField] private Sprite icon;
    
    
    public string Name => name;
    public string Title => title;
    public string Description => description;
    public Sprite Icon => icon;
    
}
