using System;
using PrimeTween;
using UnityEngine;

public class WeaponGfx : MonoBehaviour
{

    protected RailPlayer player;
    protected bool isShowing;
    private Sequence _gfxSequence;


    private void Awake()
    {
        player = GetComponentInParent<RailPlayer>();
    }

    public void Show(bool animate = true)
    {
        if (transform.localScale == Vector3.one) return;
        if (_gfxSequence.isAlive) _gfxSequence.Stop();

        isShowing = true;
        
        if (!animate)
        {
            transform.localScale = Vector3.one;
            return;
        }
        
        _gfxSequence = Sequence.Create()
            .Group(Tween.Scale(transform, Vector3.zero ,Vector3.one, 0.2f));
        
        
    }
    
    
    public void Hide(bool animate = true)
    {
        if (transform.localScale == Vector3.zero) return;
        if (_gfxSequence.isAlive) _gfxSequence.Stop();

        isShowing = false;
        
        StopAnimation();
        
        if (!animate)
        {
            transform.localScale = Vector3.zero;
            return;
        }
        
        _gfxSequence = Sequence.Create()
            .Group(Tween.Scale(transform, Vector3.one, Vector3.zero, 0.2f));
    }


    public virtual void AnimateUsage()
    {
        
    }

    public virtual void StopAnimation()
    {
        
    }
    
}