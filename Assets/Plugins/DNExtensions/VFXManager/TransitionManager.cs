using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.VFXManager
{
    public static class TransitionManager
    {
        private static Sequence activeTransition;
        private static SOVFEffectsSequence pendingOutSequence;
        private static bool isInitialized;
        
        static TransitionManager()
        {
            Initialize();
        }
        
        private static void Initialize()
        {
            if (isInitialized) return;
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            isInitialized = true;
        }
        
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!VFXManager.Instance) return;
            
            if (pendingOutSequence)
            {
                VFXManager.Instance.PlayVFX(pendingOutSequence);
                pendingOutSequence = null;
            }
            else
            {
                VFXManager.Instance.ResetActiveEffects();
            }
        }
        
        /// <summary>
        /// Transitions to a new scene with optional visual effects sequences for in and out transitions.
        /// </summary>
        public static void TransitionToScene(string sceneName, SOVFEffectsSequence vfxSequenceIn = null, SOVFEffectsSequence vfxSequenceOut = null)
        {
            if (!VFXManager.Instance)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            
            if (activeTransition.isAlive)
            {
                activeTransition.Stop();
                VFXManager.Instance.ResetActiveEffects();
            }
            

            var transitionDuration = VFXManager.Instance.PlayVFX(vfxSequenceIn);
            pendingOutSequence = vfxSequenceOut;

            activeTransition = Sequence.Create()
                .ChainDelay(transitionDuration)
                .ChainCallback(() =>
                {
                    SceneManager.LoadScene(sceneName);
                });
        }
        
        
        /// <summary>
        /// Transitions to a new scene by index with optional visual effects sequences for in and out transitions.
        /// </summary>
        public static void TransitionToScene(int sceneIndex, SOVFEffectsSequence vfxSequenceIn = null, SOVFEffectsSequence vfxSequenceOut = null)
        {
            if (!VFXManager.Instance)
            {
                SceneManager.LoadScene(sceneIndex);
                return;
            }
            
            if (activeTransition.isAlive)
            {
                activeTransition.Stop();
                VFXManager.Instance.ResetActiveEffects();
            }
            
            var transitionDuration = VFXManager.Instance.PlayVFX(vfxSequenceIn);
            pendingOutSequence = vfxSequenceOut;

            activeTransition = Sequence.Create()
                .ChainDelay(transitionDuration)
                .ChainCallback(() =>
                {
                    SceneManager.LoadScene(sceneIndex);
                });
        }
        
        /// <summary>
        /// Transitions to a new scene using a SceneField with optional visual effects sequences for in and out transitions.
        /// </summary>
        public static void TransitionToScene(SceneField scene, SOVFEffectsSequence vfxSequenceIn = null, SOVFEffectsSequence vfxSequenceOut = null)
        {
            if (!VFXManager.Instance)
            {
                scene?.LoadScene();
                return;
            }
            
            if (activeTransition.isAlive)
            {
                activeTransition.Stop();
                VFXManager.Instance.ResetActiveEffects();
            }
            
            var transitionDuration = VFXManager.Instance.PlayVFX(vfxSequenceIn);
            pendingOutSequence = vfxSequenceOut;
            

            activeTransition = Sequence.Create()
                .ChainDelay(transitionDuration)
                .ChainCallback(() =>
                {
                    scene?.LoadScene();
                });
        }
        
    }
    
    
    

}