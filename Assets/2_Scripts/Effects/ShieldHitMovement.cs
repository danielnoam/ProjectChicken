using System.Collections;
using UnityEngine;

public class ShieldHitMovement : MonoBehaviour
{
   private static readonly int HitPos = Shader.PropertyToID("_HitPos");
   private static readonly int HitColor = Shader.PropertyToID("_HitColor");
   private static readonly int DisplacementStrength = Shader.PropertyToID("_DisplacementStrength");


   [SerializeField] private AnimationCurve _DisplacementCurve;
   [SerializeField] private float _DisplacementMagnitude;
   [SerializeField] private float _LerpSpeed;
   [SerializeField] private Color _hitColor;
   [SerializeField] private Renderer[] renderers;
   //s[SerializeField] private Texture _HitTex;
   
   private Renderer _renderer;
   private Camera _camera;

   private void Awake()
   {
      _camera = Camera.main;
      //_renderer.material.SetColor("_HitColor", Color.black);
      _renderer = GetComponent<Renderer>();
   }

   private void Update()
   {
      if (!_camera) return;
      
      if (Input.GetMouseButtonDown(0))
      {
         Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
         if (Physics.Raycast(ray, out var hit))
         {
            HitShield(hit.point);
         }
      }

   }
   public void HitShield(Vector3 hitPos)
   {
      Vector3 localHitPos = transform.InverseTransformPoint(hitPos);
      
      _renderer.material.SetVector(HitPos, localHitPos);
      
      StopAllCoroutines();
      StartCoroutine(Coroutine_HitDisplacement());
     
   }

   private IEnumerator Coroutine_HitDisplacement()
   {
      float lerp = 0f;

      while (lerp < 1f)
      {
         foreach (Renderer rend in renderers)
         {
            Material mat = rend.material; 
            mat.SetFloat(DisplacementStrength, _DisplacementCurve.Evaluate(lerp) * _DisplacementMagnitude);
            mat.SetColor(HitColor, Color.Lerp(_hitColor, Color.black, lerp));
            //mat.SetTexture(_HitTex);
         }

         lerp += Time.deltaTime * _LerpSpeed;
         yield return null;
      }

      // Ensure all renderers reset
      foreach (Renderer rend in renderers)
      {
         Material mat = rend.material;
         mat.SetFloat(DisplacementStrength, 0);
         mat.SetColor(HitColor, Color.black);
      }
   }
}
