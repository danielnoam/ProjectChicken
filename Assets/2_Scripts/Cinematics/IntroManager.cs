using DNExtensions.VFXManager;

public class IntroManager : CinematicManager
{

    protected override void Start()
    {
        base.Start();
    }

    protected override void OnCinematicComplete()
    {
        SaveManager.UpdateWatchedIntro(true);
        base.OnCinematicComplete();
    }
}