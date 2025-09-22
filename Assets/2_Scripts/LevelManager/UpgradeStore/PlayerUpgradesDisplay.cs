
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PrimeTween;
using UnityEngine.UI;

public class PlayerUpgradesDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform playerUpgradesHolder;
    [SerializeField] private UpgradeInfoDisplay upgradeInfoDisplayPrefab;
    
    private readonly List<UpgradeInfoDisplay> _playerUpgrades = new List<UpgradeInfoDisplay>();
    private RectTransform _rectTransform;
    private Vector2 _fullSize;
    private Sequence _displaySequence;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _fullSize = _rectTransform.sizeDelta;
        _rectTransform.sizeDelta = new Vector2(_fullSize.x, 0);
    }

    public void UpdatePlayerUpgrades(RailPlayer player)
    {
        if (!player || player.Upgrades.Count == 0)
        {
            HideDisplay();
            return;
        }
        
        foreach (var playerUpgradeInfo in _playerUpgrades)
        {
            if (playerUpgradeInfo)
            {
                Destroy(playerUpgradeInfo.gameObject);
            }
        }
        
        _playerUpgrades.Clear();
        var playerUpgrades = player.Upgrades.Keys.ToList();
        playerUpgrades = playerUpgrades.OrderBy(upgrade => upgrade.name).ToList();

        foreach (var upgrade in playerUpgrades)
        {
            if (!upgrade) continue;
            
            var existingInfo = _playerUpgrades.FirstOrDefault(info => info && info.Upgrade == upgrade);
            if (!existingInfo)
            {
                var smallUpgradeInfo = Instantiate(upgradeInfoDisplayPrefab, playerUpgradesHolder);
                smallUpgradeInfo.SetInfo(upgrade);
                _playerUpgrades.Add(smallUpgradeInfo);
            }
        }
        
        ShowDisplay();
    }
    
    private void ShowDisplay()
    {
        if (_displaySequence.isAlive) _displaySequence.Stop();
        
        _displaySequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 1f, 0.2f))
            .Group(Tween.UISizeDelta(_rectTransform, _fullSize, 0.3f));
    }
    
    private void HideDisplay()
    {
        if (_displaySequence.isAlive) _displaySequence.Stop();
        
        _displaySequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 0f, 0.2f))
            .Group(Tween.UISizeDelta(_rectTransform, new Vector2(_fullSize.x, 0), 0.3f));
    }
    
    public void ClearUpgrades()
    {
        foreach (var playerUpgradeInfo in _playerUpgrades)
        {
            if (playerUpgradeInfo)
            {
                Destroy(playerUpgradeInfo.gameObject);
            }
        }
        
        _playerUpgrades.Clear();
        HideDisplay();
    }
    
}