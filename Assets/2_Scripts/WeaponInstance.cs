using System;
using PrimeTween;
using UnityEngine;

[Serializable]
public class WeaponInstance
{
    public SOWeapon weaponData;
    public Transform weaponGfx;
    public Transform weaponReticle;
    public Transform[] weaponBarrels;
    private Tween _reticleTween;

    public void OnWeaponSelected()
    {
        weaponGfx?.gameObject.SetActive(true);
        ToggleWeaponReticle(true);
    }
    
    public void ToggleWeaponReticle(bool state)
    {
        if (_reticleTween.isAlive) _reticleTween.Stop();
        _reticleTween = TweenReticleSize(state ? 1f :  0f, 0.5f);
    }
    

    #region Events --------------------------------------------------------------------------------------

    public void OnWeaponDeselected()
    {
        weaponGfx?.gameObject.SetActive(false);
        ToggleWeaponReticle(false);
    }

    public void OnWeaponUsed()
    {
        if (_reticleTween.isAlive) _reticleTween.Stop();
        _reticleTween = PunchReticleSize(0.25f, 0.3f);
    }

    public void OnWeaponOverheat()
    {
        if (_reticleTween.isAlive) _reticleTween.Stop();
        _reticleTween = PunchReticleSize(1f, 0.3f);
    }

    #endregion Events --------------------------------------------------------------------------------------
    

    #region Tweens -----------------------------------------------------------------------------------

    private Tween TweenReticleSize(float size, float duration)
    {
        return Tween.Scale(weaponReticle, endValue:Vector3.one * size, duration, Ease.InOutBack);
    }

    private Tween PunchReticleSize(float strength, float duration)
    {
        weaponReticle.localScale = Vector3.one;
        return Tween.PunchScale(weaponReticle,Vector3.one * strength, duration: duration);
    }

    #endregion Tweens -----------------------------------------------------------------------------------

}