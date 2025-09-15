using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using VInspector;

public class BackgroundObjectsAnimator : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private List<Transform> objectsToAnimate = new List<Transform>();
    
    [Header("Animation Settings")]
    [SerializeField] private float endZPosition = -50f;
    [SerializeField] private float duration = 3f;
    [SerializeField] private Ease ease = Ease.InOutQuad;
    
    [Header("Timing")]
    [SerializeField] private float globalStartDelay;
    [SerializeField] private float indexDelay = 0.1f;
    
    private Sequence _backgroundSequence;
    private readonly List<Vector3> _startPositions = new List<Vector3>();
    
    private void Awake()
    {
        StoreStartPositions();
    }
    
    private void StoreStartPositions()
    {
        _startPositions.Clear();
        
        foreach (var obj in objectsToAnimate)
        {
            if (obj != null)
            {
                _startPositions.Add(obj.position);
            }
        }
    }
    
    [Button]
    public void StartAnimation()
    {
        if (_backgroundSequence.isAlive)
        {
            _backgroundSequence.Stop();
        }
        
        _backgroundSequence = Sequence.Create()
            .ChainDelay(globalStartDelay)
            .ChainCallback(()=>{}); // Just to have a slight delay before starting the grouped tweens.
        

        
        for (int i = 0; i < objectsToAnimate.Count && i < _startPositions.Count; i++)
        {
            if (!objectsToAnimate[i]) continue;
            
            Vector3 startPos = _startPositions[i];
            Vector3 endPos = new Vector3(startPos.x, startPos.y, endZPosition);
            float totalDelay = i * indexDelay;
            
            _backgroundSequence.Group(
                Tween.Position(
                    objectsToAnimate[i], 
                    startDelay: totalDelay,
                    startValue: startPos,
                    endValue: endPos, 
                    duration: duration, 
                    ease: ease
                )
            );
        }
    }
    
    public void StopAnimation()
    {
        if (_backgroundSequence.isAlive)
        {
            _backgroundSequence.Stop();
        }
    }
}