using TMPro;
using UnityEngine;
using VInspector;

public class CreditsText : MonoBehaviour
{
    public TextMeshProUGUI creditsText;

    [Header("Main Title")]
    [Min(0)] public int titleSize = 48;
    public bool titleBold;
    [Min(0)] public int spaceLinesAfterTitle = 12;

    [Header("Original Game")]
    [Min(0)] public int spaceLinesAfterOriginalGame = 10;
    
    [Header("Studio Name")]
    [Min(0)] public int studioSize = 36;
    public bool studioBold;
    [Min(0)] public int spaceLinesBetweenRoles = 1;
    [Min(0)] public int spaceLinesAfterTeam = 10;

    [Header("Third Party Assets")]
    [Min(0)] public int spaceLinesAfterThirdParty = 15;
    
    [Header("Thanks")]
    [Min(0)] public int spaceLinesAfterThanks = 35;
    
    [Header("Major Headers")]
    [Min(0)] public int majorHeaderSize = 32;
    public bool majorHeaderBold;

    [Header("Minor Headers")]
    [Min(0)] public int minorHeaderSize = 28;
    public bool minorHeaderBold;

    [Header("Fade Settings")]
    [Tooltip("Y position where fade starts (screen space, 0-1)")]
    public float fadeStartY = 0.8f;
    
    [Tooltip("Y position where text is fully transparent (screen space, 0-1)")]
    public float fadeEndY = 1.0f;

    private Camera _camera;


    private void Awake()
    {
        _camera = Camera.main;
    }

    private void OnValidate()
    {
        SetCreditsText();
    }
    
    private void LateUpdate()
    {
        creditsText.ForceMeshUpdate();
        
        var textInfo = creditsText.textInfo;
        
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            
            if (!charInfo.isVisible)
                continue;
            
            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;
            
            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;
            

            Vector3 charPos = (charInfo.topLeft + charInfo.bottomRight) / 2f;
            if (_camera)
            {
                Vector3 screenPos = _camera.WorldToScreenPoint(creditsText.transform.TransformPoint(charPos));
                float normalizedY = screenPos.y / Screen.height;
            
                // Calculate alpha for top fade
                float alpha = 1f;
                if (normalizedY > fadeStartY)
                {
                    alpha = Mathf.InverseLerp(fadeEndY, fadeStartY, normalizedY);
                }
            
                byte alphaValue = (byte)(alpha * 255);
            
                // Apply alpha to all 4 vertices of the character
                vertexColors[vertexIndex + 0].a = alphaValue;
                vertexColors[vertexIndex + 1].a = alphaValue;
                vertexColors[vertexIndex + 2].a = alphaValue;
                vertexColors[vertexIndex + 3].a = alphaValue;
            }
        }
        
        creditsText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
    
    
    private string GetSpacing(int lines)
    {
        return new string('\n', lines);
    }
    
    
    [Button]
    private void SetCreditsText()
    {
        string titleFormat = titleBold ? "<b>" : "";
        string titleClose = titleBold ? "</b>" : "";
        
        string studioFormat = studioBold ? "<b>" : "";
        string studioClose = studioBold ? "</b>" : "";
        
        string majorFormat = majorHeaderBold ? "<b>" : "";
        string majorClose = majorHeaderBold ? "</b>" : "";
        
        string minorFormat = minorHeaderBold ? "<b>" : "";
        string minorClose = minorHeaderBold ? "</b>" : "";

        creditsText.text = 
        $@"<size={titleSize}>{titleFormat}CHICKEN INVADERS REMAKE{titleClose}</size>

        <size={studioSize}>{studioFormat}A Variety Bucket Production{studioClose}</size>
        {GetSpacing(spaceLinesAfterTitle)}
        <size={majorHeaderSize}>{majorFormat}Original Game{majorClose}</size>
        
        Chicken Invaders
        Created by Konstantinos Prouskas & InterAction studios
        {GetSpacing(spaceLinesAfterOriginalGame)}
        <size={majorHeaderSize}>{majorFormat}Development Team{majorClose}</size>
        
        <size={minorHeaderSize}>{minorFormat}Art{minorClose}</size>
        Shahar Dagan
        Or Israel
        {GetSpacing(spaceLinesBetweenRoles)}
        <size={minorHeaderSize}>{minorFormat}Visual Effects{minorClose}</size>
        Dekel Carolla
        {GetSpacing(spaceLinesBetweenRoles)}
        <size={minorHeaderSize}>{minorFormat}Programming{minorClose}</size>
        Tay Ahuva
        Daniel Noam
        {GetSpacing(spaceLinesAfterTeam)}
        <size={majorHeaderSize}>{majorFormat}Third-Party Assets{majorClose}</size>
        
        Cartoon Chicken by shawshank73
        Rooster Call by DRAGON STUDIO
        Rooster Crowing by DRAGON STUDIO
        Egg Crack by u_xg7ssi08yr
        Level Up by Sunovia
        High Speed by Universfield
        Swoosh 08 by Universfield
        Woosh FX by soundreality
        Cinematic Boom by LordSonny
        Explosion 1 by Star Wars SFX Archive
        Fire Magic 4 by yodguard
        Space Explosion with Reverb by morganpurkis
        Morse Code Samples by InMotion Audio
        Blaster Rifle Overheat Sound by Fortnite
        Item 02 by lilmati
        Laser 3 by nsstudios
        Retro Laser Shot by rubberduck9999
        SFX Laser Shot by bolkmar
        Tiger Claw Custom Laser by arthninja
        Unfas Laser Weapon Sounds by unfa
        Minimalist Button Hover Sound Effect by Lesiakower9
        UI Sound Pack 04 by Kpow Audio
        Galaxy Materials by SineVFX
        Quick Outline by Chris Nolet
        UI Buttons Prompts by Kenney Assets
        Editable Asset Attribute by Not Good Enough
        PrimeTween by Kyrylo Kuzyk
        TimeScale Toolbar by bl4st
        vFolders 2 by kubacho lab
        vHierarchy 2 by kubacho lab
        vInspector 2 by kubacho lab
        TMPEffects by Luca Weist
        Hot Reload by The Naughty Cult
        SceneReference by Eflatun
        {GetSpacing(spaceLinesAfterThirdParty)}
        <size={majorHeaderSize}>{majorFormat}Special Thanks{majorClose}</size>
        Israel Animation College
        for their support and guidance throughout this project
        {GetSpacing(spaceLinesAfterThanks)}
        Thanks for playing!";
    }

}