using UnityEngine;
using UnityEngine.Playables;
using VInspector;


public class IntroManager : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private PlayableDirector playableDirector;
    
    

    
    
    [Button]
    private void PlayIntro()
    {
        playableDirector.Play();
    }

    [Button]
    private void StopIntro()
    {
        playableDirector.Stop();

    }

    [Button]
    private void ToggleIntro()
    {
        if (playableDirector.state == PlayState.Playing)
        {
            playableDirector.Pause();
        }
        else
        {
            playableDirector.Play();
        }
    }
}
