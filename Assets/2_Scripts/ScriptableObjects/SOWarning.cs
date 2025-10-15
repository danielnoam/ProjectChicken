using PrimeTween;
using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "New Warning", menuName = "Scriptable Objects/New Warning")]
public class SOWarning : ScriptableObject
{
    [Header("Warning Data")]
    [SerializeField] private Sprite icon;
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private Color color = Color.white;
    [SerializeField, TextArea(1, 3)] private string message;
    [SerializeField, Min(0.5f)] private float duration = 3f;

    [Header("Show Animation")]
    [SerializeField] private bool animateShowShake;
    [ShowIf("animateShowShake")]
    [SerializeField] private Vector3 showShakeStrength = new Vector3(5, 5, 0);
    [SerializeField] private float showShakeFrequency = 10f;
    [SerializeField] private float showShakeDuration = 0.5f;
    [SerializeField] private Ease showShakeEase = Ease.Default;
    [EndIf]

    [Space(5)]
    [SerializeField] private bool animateShowScale;
    [ShowIf("animateShowScale")]
    [SerializeField] private Vector3 showScaleOffset = Vector3.zero;
    [SerializeField] private float showScaleDuration = 0.5f;
    [SerializeField] private Ease showScaleEase = Ease.OutBack;
    [EndIf]

    [Header("Hide Animation")]
    [SerializeField] private bool animateHideShake;
    [ShowIf("animateHideShake")]
    [SerializeField] private Vector3 hideShakeStrength = new Vector3(5, 5, 0);
    [SerializeField] private float hideShakeFrequency = 10f;
    [SerializeField] private float hideShakeDuration = 0.5f;
    [SerializeField] private Ease hideShakeEase = Ease.Default;
    [EndIf]

    [Space(5)]
    [SerializeField] private bool animateHideScale;
    [ShowIf("animateHideScale")]
    [SerializeField] private Vector3 hideScaleOffset = Vector3.zero;
    [SerializeField] private float hideScaleDuration = 0.3f;
    [SerializeField] private Ease hideScaleEase = Ease.InBack;
    [EndIf]

    // Helper methods for VInspector ShowIf conditions
    // (No longer needed, but kept for potential future use)

    public enum PositionEffectType { Offset, Shake }

    // Public Properties
    public Sprite Icon => icon;
    public AudioClip AudioClip => audioClip;
    public Color Color => color;
    public string Message => message;
    public float Duration => duration;

    // Show Animation Properties
    public bool AnimateShowShake => animateShowShake;
    public Vector3 ShowShakeStrength => showShakeStrength;
    public float ShowShakeFrequency => showShakeFrequency;
    public float ShowShakeDuration => showShakeDuration;
    public Ease ShowShakeEase => showShakeEase;

    public bool AnimateShowScale => animateShowScale;
    public Vector3 ShowScaleOffset => showScaleOffset;
    public float ShowScaleDuration => showScaleDuration;
    public Ease ShowScaleEase => showScaleEase;

    // Hide Animation Properties
    public bool AnimateHideShake => animateHideShake;
    public Vector3 HideShakeStrength => hideShakeStrength;
    public float HideShakeFrequency => hideShakeFrequency;
    public float HideShakeDuration => hideShakeDuration;
    public Ease HideShakeEase => hideShakeEase;

    public bool AnimateHideScale => animateHideScale;
    public Vector3 HideScaleOffset => hideScaleOffset;
    public float HideScaleDuration => hideScaleDuration;
    public Ease HideScaleEase => hideScaleEase;
    
    
    [Button]
    private void AddWarning()
    {
        WarningSystemManager.Instance?.AddWarning(this);
    }
    
    [Button]
    private void PlayWarning()
    {
        WarningSystemManager.Instance?.PlayWarning(this);
    }
}