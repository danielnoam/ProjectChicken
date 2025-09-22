using System;
using PrimeTween;
using UnityEngine;

public class CaptainCock : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float animationDuration = 1.5f;
    [SerializeField] private Ease animationEase = Ease.InOutBack;
    [SerializeField] private float outOfScreenYPosition = -150;
    [SerializeField] private float spinsOnOpen = 3;
    
    [Header("Idle Animation")]
    [SerializeField] private float bobbingSpeed = 2;
    [SerializeField] private float bobbingAmplitude = 1;
    
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

    private void Update()
    {
        if (!_animationSequence.isAlive)
        {
            transform.localPosition = _startPosition + new Vector3(0, Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmplitude, 0);
        }
    }


    public void OnStoreOpen()
    {
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
            .Group(Tween.LocalPositionY(transform, outOfScreenYPosition, _startPosition.y, animationDuration, animationEase))
            .Group(Tween.LocalEulerAngles(transform,_startRotation, new Vector3(0,_startRotation.y + spinsOnOpen*360,0), animationDuration * 0.9f, animationEase))
            ;
    }

    public void OnStoreClose()
    {
        if (_animationSequence.isAlive) _animationSequence.Stop();
        _animationSequence = Sequence.Create()
                .Group(Tween.LocalPositionY(transform,_startPosition.y,outOfScreenYPosition, animationDuration,animationEase))
                .Group(Tween.LocalEulerAngles(transform,transform.eulerAngles, new Vector3(0,_startRotation.y + spinsOnOpen*-360,0), animationDuration * 0.9f, animationEase))
            ;
    }
}