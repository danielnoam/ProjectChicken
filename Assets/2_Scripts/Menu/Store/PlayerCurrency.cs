using System;
using CustomAttribute;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using VInspector;

public class PlayerCurrency : MonoBehaviour
{

     [SerializeField] private TextMeshProUGUI currencyText;
     
     private int _currentCurrency;


     private void Start()
     {
          _currentCurrency = SaveManager.GetCurrency();
     }

     private void Update()
     {
          if (!currencyText) return;
          
          
          currencyText.text = _currentCurrency.ToString();
     }
}
