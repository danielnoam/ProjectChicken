using System.Collections;
using UnityEngine;

public class ShieldHitMovement : MonoBehaviour
{
   Renderer _renderer;
   [SerializeField] private AnimationCurve _DisplacementCurve;
   [SerializeField] float _DisplacementMagnitude;
   [SerializeField] float _LerpSpeed;

   void Start()
   {
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
      _renderer.material.SetVector("_HitPos", hitPos);
      StopAllCoroutines();
      StartCoroutine(Coroutine_HitDisplacement());
     
   }

   IEnumerator Coroutine_HitDisplacement()
   {
      float lerp = 0;
      while (lerp < 1)
      {
         _renderer.material.SetFloat("_DisplacementStrength", _DisplacementCurve.Evaluate(lerp) * 
                                                              _DisplacementMagnitude);
         lerp += Time.deltaTime * _LerpSpeed;
         yield return null;
      }
   }
}
