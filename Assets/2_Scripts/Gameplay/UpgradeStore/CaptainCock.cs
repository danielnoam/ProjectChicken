using PrimeTween;
using UnityEngine;

public class CaptainCock : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float animationDuration = 1.5f;
    [SerializeField] private Ease animationEase = Ease.InOutBack;
    [SerializeField] private float yOffset = -150;
    
    
    private Vector3 _startPosition;
    private Vector3 _startScale;
    private Vector3 _startRotation;
    private Sequence _animationSequence;
    

    private void Awake()
    {
        _startPosition = transform.localPosition;
        _startScale = transform.localScale;
        _startRotation = transform.localEulerAngles;

    }

    public void OnStoreOpen()
    {
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
            .Group(Tween.LocalPositionY(transform, yOffset, _startPosition.y, animationDuration, animationEase))
            .Group(Tween.LocalEulerAngles(transform, Vector3.forward * 180 ,_startRotation, animationDuration, animationEase))
            ;
    }

    public void OnStoreClose()
    {
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
                .Group(Tween.LocalPositionY(transform,_startPosition.y,yOffset, animationDuration,animationEase))
                .Group(Tween.LocalEulerAngles(transform, _startRotation, Vector3.forward * 180, animationDuration, animationEase))
            ;
    }
}