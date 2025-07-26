using PrimeTween;
using UnityEngine;
using VInspector;


namespace DNExtensions.VFXManager
{
    
    [CreateAssetMenu(fileName = "VFX Sequence", menuName = "Scriptable Objects/New VFX Sequence")]
    public class SOVFEffectsSequence : ScriptableObject
    {
        [Header("Sequence Settings")]
        [SerializeField, Min(0f)] private float sequenceDuration = 1f;
        [SerializeReference] private VFEffectsEffectBase[] effects;


        private Sequence _sequence;


        [Button]
        public float PlayEffects()
        {
            if (_sequence.isAlive) _sequence.Stop();
            
            VFXManager.Instance?.ResetActiveEffects();
            
            foreach (var effect in effects)
            {
                effect?.OnPlayEffect(sequenceDuration);
            }


            return sequenceDuration;
        }
        
        

        [Button]
        public void ResetEffects()
        {
            foreach (var effect in effects)
            {
                effect?.OnResetEffect();
            }
        }
    }
    
    
    
    
}