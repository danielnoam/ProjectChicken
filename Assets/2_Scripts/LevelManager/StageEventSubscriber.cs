using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class StageSubscribtion
{
    public SOLevelStage stageToSubscribe;
    public UnityEvent onStageStart;
    public UnityEvent onStageEnd;
    
    
}

public class StageEventSubscriber : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private StageSubscribtion[] subscribtions;


    private void OnEnable()
    {
        foreach (var subscribtion in subscribtions)
        {
            if (!subscribtion.stageToSubscribe) continue;
            subscribtion.stageToSubscribe.OnStageStarted += subscribtion.onStageStart.Invoke;
            subscribtion.stageToSubscribe.OnStageEnded += subscribtion.onStageEnd.Invoke;
        }
    }
    
    private void OnDisable()
    {
        foreach (var subscribtion in subscribtions)
        {
            if (!subscribtion.stageToSubscribe) continue;
            Debug.Log("Unsubscribing from stage: " + subscribtion.stageToSubscribe.name);
            subscribtion.stageToSubscribe.OnStageStarted -= subscribtion.onStageStart.Invoke;
            subscribtion.stageToSubscribe.OnStageEnded -= subscribtion.onStageEnd.Invoke;
        }
    }
    

}