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

public class StageSubscriber : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private StageSubscribtion[] subscribtions; 


    private void OnEnable()
    {
        foreach (var stage in subscribtions)
        {
            if (!stage.stageToSubscribe) continue;
            stage.stageToSubscribe.OnStageStarted += stage.onStageStart.Invoke;
            stage.stageToSubscribe.OnStageEnded += stage.onStageEnd.Invoke;
        }
    }
    
    private void OnDisable()
    {
        foreach (var stage in subscribtions)
        {
            if (!stage.stageToSubscribe) continue;

            stage.stageToSubscribe.OnStageStarted -= stage.onStageStart.Invoke;
            stage.stageToSubscribe.OnStageEnded -= stage.onStageEnd.Invoke;
        }
    }
    

}