using System.Collections;
using UnityEngine;

public class ShieldHitMovement : MonoBehaviour
{
    private static readonly int Alpha = Shader.PropertyToID("_Alpha");
    private static readonly int HitPos = Shader.PropertyToID("_HitPos");
    private static readonly int HitColor = Shader.PropertyToID("_HitColor");
    private static readonly int DisplacementStrength = Shader.PropertyToID("_DisplacementStrength");
    
    
    [SerializeField] private new Renderer renderer;
    [SerializeField] private float displacementMagnitude;
    [SerializeField] private AnimationCurve displacementCurve;
    [SerializeField] private float displacementLerpSpeed;
    [SerializeField] private Color hitColor;
    [SerializeField] private float colorLerpSpeed;


  
    private Camera _camera;
    private Material _material;
    private Coroutine _hitDisplacementCoroutine;

    private void Awake()
    {
        _camera = Camera.main;
        _material = renderer.material;
    }

    // private void Update()
    // {
    //     if (!_camera) return;
    //  
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out var hit))
    //         {
    //             HitShield(hit.point);
    //         }
    //     }
    // }


    private IEnumerator HitDisplacementRoutine()
    {
        float colorLerp = 0f;
        float displacementLerp = 0f;


        while (colorLerp < 1f || displacementLerp < 1f)
        {
            if (colorLerp < 1f)
            {
                _material.SetColor(HitColor, Color.Lerp(hitColor, Color.black, colorLerp));
                colorLerp += Time.deltaTime * colorLerpSpeed;
            }
            
            if (displacementLerp < 1f)
            {
                _material.SetFloat(DisplacementStrength, displacementCurve.Evaluate(displacementLerp) * displacementMagnitude);
                displacementLerp += Time.deltaTime * displacementLerpSpeed;
            }

            yield return null;
        }


        _material.SetFloat(DisplacementStrength, 0);
        _material.SetColor(HitColor, Color.black);
        _hitDisplacementCoroutine = null;
    }
    
    public void HitShield(Vector3 hitPos)
    {
        Vector3 localHitPos = transform.InverseTransformPoint(hitPos);
     
        _material.SetVector(HitPos, localHitPos);
        
        if (_hitDisplacementCoroutine != null)
        {
            StopCoroutine(_hitDisplacementCoroutine);
        }
        _hitDisplacementCoroutine = StartCoroutine(HitDisplacementRoutine());
    }

    public void SetAlpha(float alpha)
    {
        _material.SetFloat(Alpha, alpha);
    }
}