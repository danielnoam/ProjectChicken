using System;
using UnityEngine;
using System.Collections;

public class PassthroughObstacle : MonoBehaviour
{

    
    
    [Header("Scaling")]
    [SerializeField] private float scalingDuration = 2f;
    [SerializeField] private bool autoDetectScaleDuration = true;
    
    [Header("References")]
    [SerializeField] private GameObject centerObject;
    
    private bool _hasBeenCentered;
    private float _targetZ;
    
    public Transform CenterObjectTransform => centerObject ? centerObject.transform : transform;
    
    
    
    private void Start()
    {
        InitialCenter();
    }
    
    private void OnDisable()
    {
        StopAllCoroutines();
        _hasBeenCentered = false;
    }
    
    private void OnEnable()
    {
        _hasBeenCentered = false;
        
        if (centerObject)
        {
            InitialCenter();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out RailPlayer player))
        {
            player.Health.TakeDamage(100f, 5f);
            Vector3 moveDirection = (player.transform.position - CenterObjectTransform.position).normalized;
            player.Movement.Push(-moveDirection, 3f);
        }
        
        if (other.TryGetComponent<ChickenStateController>(out var chicken))
        {
            chicken.TakeDamage(100);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out RailPlayer player))
        {
            Vector3 moveDirection = (player.transform.position - CenterObjectTransform.position).normalized;
            player.Movement.Push(-moveDirection, 1f);
        }
    }

    private void InitialCenter()
    {
        if (_hasBeenCentered) return;
        
        if (!centerObject)
        {
            Debug.LogWarning("No center object assigned on " + gameObject.name);
            return;
        }
        
        _targetZ = transform.position.z;
        
        if (autoDetectScaleDuration)
        {
            MonoBehaviour spaceItemBehavior = GetComponent<MonoBehaviour>();
            if (spaceItemBehavior)
            {
                var scaleDurationField = spaceItemBehavior.GetType().GetField("scaleDuration");
                if (scaleDurationField != null)
                {
                    scalingDuration = (float)scaleDurationField.GetValue(spaceItemBehavior);
                }
            }
        }
        
        CenterObjectAtTarget();
        
        StartCoroutine(MonitorPositionDuringScaling());
        
        _hasBeenCentered = true;
    }
    
    private void CenterObjectAtTarget()
    {
        if (!centerObject) return;
        
        Vector3 centerWorldPosition = centerObject.transform.position;
        
        Vector3 targetPosition = new Vector3(0f, 0f, _targetZ);
        
        Vector3 offset = targetPosition - centerWorldPosition;
        
        transform.position += offset;
    }
    
    private IEnumerator MonitorPositionDuringScaling()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < scalingDuration)
        {
            CenterObjectAtTarget();
            
            yield return null;
            elapsedTime += Time.deltaTime;
        }
        
        CenterObjectAtTarget();
    }
}