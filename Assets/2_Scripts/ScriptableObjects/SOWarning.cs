using DNExtensions;
using PrimeTween;
using UnityEngine;
using VInspector;

[CreateAssetMenu(fileName = "New Warning", menuName = "Scriptable Objects/New Warning")]
public class SOWarning : ScriptableObject
{
    [Header("Warning Data")]
    [SerializeField] private Sprite icon;
    [SerializeField] private SOAudioEvent warningSfx;
    [SerializeField] private Color iconColor = Color.white;
    [SerializeField] private Color backgroundColor = Color.white;
    [SerializeField, TextArea(1, 3)] private string message;
    [SerializeField, Min(0.5f)] private float duration = 3f;

    [Header("Show Animation")]
    [SerializeField] private Vector3 showShakeStrength = new Vector3(5, 5, 0);
    [SerializeField] private float showShakeFrequency = 10f;
    [SerializeField] private float showShakeDuration = 0.5f;
    [SerializeField] private Ease showShakeEase = Ease.Default;
    [Space(5)]
    [SerializeField] private Vector3 showScaleOffset = Vector3.zero;
    [SerializeField] private float showScaleDuration = 0.5f;
    [SerializeField] private Ease showScaleEase = Ease.OutBack;


    [Header("Hide Animation")]
    [SerializeField] private Vector3 hideShakeStrength = new Vector3(5, 5, 0);
    [SerializeField] private float hideShakeFrequency = 10f;
    [SerializeField] private float hideShakeDuration = 0.5f;
    [SerializeField] private Ease hideShakeEase = Ease.Default;
    [Space(5)]
    [SerializeField] private Vector3 hideScaleOffset = Vector3.zero;
    [SerializeField] private float hideScaleDuration = 0.3f;
    [SerializeField] private Ease hideScaleEase = Ease.InBack;


    



    public Sprite Icon => icon;
    public SOAudioEvent WarningSfx => warningSfx;
    public Color BackgroundColor => backgroundColor;
    public Color IconColor => iconColor;
    public string Message => message;
    public float Duration => duration;

    // Show Animation Properties
    public Vector3 ShowShakeStrength => showShakeStrength;
    public float ShowShakeFrequency => showShakeFrequency;
    public float ShowShakeDuration => showShakeDuration;
    public Ease ShowShakeEase => showShakeEase;
    
    public Vector3 ShowScaleOffset => showScaleOffset;
    public float ShowScaleDuration => showScaleDuration;
    public Ease ShowScaleEase => showScaleEase;

    // Hide Animation Properties
    public Vector3 HideShakeStrength => hideShakeStrength;
    public float HideShakeFrequency => hideShakeFrequency;
    public float HideShakeDuration => hideShakeDuration;
    public Ease HideShakeEase => hideShakeEase;
    
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