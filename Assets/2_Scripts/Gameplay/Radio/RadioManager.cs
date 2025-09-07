using System;
using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RadioManager : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private RadioMessageUI radioMessageUI;
    [SerializeField, Self(Flag.Editable)] private AudioSource audioSource;
    [SerializeField, Scene(Flag.EditableAnywhere)] private LevelManager levelManager;
    [SerializeField, Scene(Flag.EditableAnywhere)] private RailPlayer player;

    private readonly List<SORadioMessage> _messages = new List<SORadioMessage>();
    private SORadioMessage _currentMessage;
    private bool _messagePlaying;
    
    
    private void OnValidate()
    {
        if (!levelManager) levelManager = FindFirstObjectByType<LevelManager>();
        if (!player) player = FindFirstObjectByType<RailPlayer>();
        this.ValidateRefs();
    }

    private void Update()
    {
        if (_messagePlaying || _messages.Count == 0) return;

        PlayNextMessage();
    }

    public void ReceiveMessage(SORadioMessage message)
    {
        if (!message) return;
        
        if (message.IsImportant)
        {
            if (_messagePlaying)
            {
                _currentMessage?.AudioEvent?.Stop(audioSource);
                _messages.Insert(0, _currentMessage);
                radioMessageUI.HideMessage();
            }
            PlayMessage(message);
        }
        else
        {
            _messages.Add(message);
        }
    }


    private void PlayNextMessage()
    {
        if (_messages.Count == 0) return;

        var message = _messages[0];
        _messages.RemoveAt(0);
        PlayMessage(message);

    }

    private void PlayMessage(SORadioMessage message)
    {
        _messagePlaying = true;
        _currentMessage = message;
        message.AudioEvent?.Play(audioSource);
        radioMessageUI.ShowMessage(message);
    }
}
