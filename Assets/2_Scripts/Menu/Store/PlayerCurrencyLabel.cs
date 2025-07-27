using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerCurrencyLabel : MonoBehaviour
{

     [SerializeField] private TextMeshProUGUI currencyText;
     
     
     private readonly List<MenuElementStoreShelf> _storeShelves = new List<MenuElementStoreShelf>();
     private int _currentCurrency;

     private void Awake()
     {
          var storeShelvesInScene = FindObjectsByType<MenuElementStoreShelf>(FindObjectsSortMode.None);
          foreach (var shelf in storeShelvesInScene)
          {
              _storeShelves.Add(shelf); 
          }
     }

     private void OnEnable()
     {
          foreach (var shelf in _storeShelves)
          {
               shelf.OnStoreItemBoughtEvent +=  (var ) => { _currentCurrency = SaveManager.GetCurrency(); };
          }
     }
     
     private void OnDisable()
     {
          foreach (var shelf in _storeShelves)
          {
               shelf.OnStoreItemBoughtEvent -= (var ) => { _currentCurrency = SaveManager.GetCurrency(); };
          }
     }

     private void Start()
     {
          _currentCurrency = SaveManager.GetCurrency();
     }

     private void Update()
     {
          if (!currencyText) return;
          
          currencyText.text = _currentCurrency.ToString();
          
          
          if (Input.GetKeyUp(KeyCode.F1))
          {
               SaveManager.UpdatePlayerCurrency(_currentCurrency + 50);
               _currentCurrency = SaveManager.GetCurrency();
          }
     }
}
