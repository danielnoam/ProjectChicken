using TMPro;
using UnityEngine;
using VInspector;

public class CreditsText : MonoBehaviour
{
    public TextMeshProUGUI creditsText;

    [Header("Title")]
    public int titleSize = 48;
    public bool titleBold;

    [Header("Studio Name")]
    public int studioSize = 36;
    public bool studioBold;

    [Header("Major Headers")]
    public int majorHeaderSize = 32;
    public bool majorHeaderBold;

    [Header("Minor Headers")]
    public int minorHeaderSize = 28;
    public bool minorHeaderBold;

    [Header("Spacing")]
    [Range(0, 35)] public int afterStudio = 8;
    [Range(0, 35)] public int afterOriginalGame = 3;
    [Range(0, 35)] public int betweenRoles = 1;
    [Range(0, 35)] public int afterTeam = 8;
    [Range(0, 35)] public int afterThirdParty = 11;
    [Range(0, 55)] public int afterThanks = 35;

    
    private void OnValidate()
    {
        SetCreditsText();
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
{GetSpacing(afterStudio)}
<size={majorHeaderSize}>{majorFormat}Original Game{majorClose}</size>

Chicken Invaders
Created by InterAction studios
{GetSpacing(afterOriginalGame)}
<size={majorHeaderSize}>{majorFormat}Development Team{majorClose}</size>

<size={minorHeaderSize}>{minorFormat}Art{minorClose}</size>
Shahar Dagan
Or Israel
{GetSpacing(betweenRoles)}
<size={minorHeaderSize}>{minorFormat}Visual Effects{minorClose}</size>
Dekel Carolla
{GetSpacing(betweenRoles)}
<size={minorHeaderSize}>{minorFormat}Programming{minorClose}</size>
Tay Ahuva
Daniel Noam
{GetSpacing(afterTeam)}
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
{GetSpacing(afterThirdParty)}
<size={majorHeaderSize}>{majorFormat}Special Thanks{majorClose}</size>
Israel Animation College
for their support and guidance throughout this project
{GetSpacing(afterThanks)}
Thanks for playing!";
    }

    private string GetSpacing(int lines)
    {
        return new string('\n', lines);
    }
}