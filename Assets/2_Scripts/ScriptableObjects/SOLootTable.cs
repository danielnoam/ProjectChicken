using DNExtensions;
using UnityEngine;
using VInspector;



[CreateAssetMenu(fileName = "New LootTable", menuName = "Scriptable Objects/New Loot Table")]
public class SOLootTable : ScriptableObject
{
    [Header("Loot Table")]
    [SerializeField] private ChanceList<Resource> resources = new ChanceList<Resource>();


    public ChanceList<Resource> Resources => resources;
    public Resource RandomResource => resources.GetRandomItem();

}