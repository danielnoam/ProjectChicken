using System.Collections;
using UnityEngine;

public class ShieldHitMovement : MonoBehaviour
{
   Renderer _renderer;
   [SerializeField] private AnimationCurve _DisplacementCurve;
   [SerializeField] float _DisplacementMagnitude;
   [SerializeField] float _LerpSpeed;
   [SerializeField] private Color _hitColor;
   [SerializeField] private Renderer[] renderers;
   //s[SerializeField] private Texture _HitTex;

   void Start()
   { 
      //_renderer.material.SetColor("_HitColor", Color.black);
      _renderer = GetComponent<Renderer>();
   }

   void Update()
   {
      if (Input.GetMouseButtonDown(0))
      {
         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
         RaycastHit hit;
         if (Physics.Raycast(ray, out hit))
         {
            HitShield(hit.point);
         }
      }
   }
   public void HitShield(Vector3 hitPos)
   {
      Vector3 localHitPos = transform.InverseTransformPoint(hitPos);
      
      _renderer.material.SetVector("_HitPos", localHitPos);
      
      StopAllCoroutines();
      StartCoroutine(Coroutine_HitDisplacement());
     
   }

   IEnumerator Coroutine_HitDisplacement()
   {
      float lerp = 0f;

      while (lerp < 1f)
      {
         foreach (Renderer rend in renderers)
         {
            Material mat = rend.material; 
            mat.SetFloat("_DisplacementStrength", _DisplacementCurve.Evaluate(lerp) * _DisplacementMagnitude);
            mat.SetColor("_HitColor", Color.Lerp(_hitColor, Color.black, lerp));
            //mat.SetTexture(_HitTex);
         }

         lerp += Time.deltaTime * _LerpSpeed;
         yield return null;
      }

      // Ensure all renderers reset
      foreach (Renderer rend in renderers)
      {
         Material mat = rend.material;
         mat.SetFloat("_DisplacementStrength", 0);
         mat.SetColor("_HitColor", Color.black);
      }
   }
}
