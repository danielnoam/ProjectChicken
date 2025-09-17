using System;
using DNExtensions;
using PrimeTween;
using TMPEffects.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class RadioMessageUI : MonoBehaviour
{
        [Header("Settings")]
        [SerializeField] private float showDuration = 0.5f;
        [SerializeField] private float hideDuration = 0.5f;
        [SerializeField] private Ease ease = Ease.InOutQuart;
        [SerializeField] private Vector2 hiddenPosition = new Vector2(0,0);
        [SerializeField] private SOAudioEvent messageWriteSfx;
        
        
        [Header("References")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Image senderNameImage;
        [SerializeField] private TextMeshProUGUI senderNameText;
        [SerializeField] private TextMeshProUGUI senderTitleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TMPWriter messageWriter;
        
        private Sequence _hideSequence;
        private Sequence _showSequence;
        private Vector2 _shownPosition;
        private RectTransform _messageUIRectTransform;
        private bool _isVisible;
        
        public event Action OnMessageHidden;
        public event Action OnMessageCompleted;

        private void Awake()
        {
                _messageUIRectTransform = (RectTransform)transform;
                _shownPosition = _messageUIRectTransform.anchoredPosition;
                _messageUIRectTransform.anchoredPosition = hiddenPosition;
        }

        public void ShowMessage(SORadioMessage message)
        {
                if (!message) return;
                
                if (_showSequence.isAlive) _showSequence.Stop();
                if (_hideSequence.isAlive) _hideSequence.Stop();
                
                _showSequence = Sequence.Create();
                
                if (_isVisible)
                {
                        _showSequence.ChainDelay(hideDuration);
                }
                
                _showSequence.ChainCallback(() =>
                        {
                                SetupMessageContent(message);
                        });
                        
                _showSequence.Chain(Tween.UIAnchoredPosition(_messageUIRectTransform, hiddenPosition, _shownPosition, showDuration, ease));
                _showSequence.ChainCallback(() => _isVisible = true);
                _showSequence.ChainDelay(message.Duration);
                _showSequence.OnComplete(() => OnMessageCompleted?.Invoke());
        }

        public void UpdateMessageOnly(SORadioMessage message)
        {
                if (!message) return;
                
                SetupMessageContent(message);
                
                if (_showSequence.isAlive) _showSequence.Stop();
                _showSequence = Sequence.Create();
                _showSequence.ChainDelay(message.Duration);
                _showSequence.OnComplete(() => OnMessageCompleted?.Invoke()); 
        }

        public void HideMessage()
        {
                if (!_isVisible) return;
                
                if (_hideSequence.isAlive) _hideSequence.Stop();
                if (_showSequence.isAlive) _showSequence.Stop();

                _hideSequence = Sequence.Create()
                        .Chain(Tween.UIAnchoredPosition(_messageUIRectTransform, hiddenPosition, hideDuration, ease))
                        .OnComplete(() => 
                        {
                                _isVisible = false;
                                OnMessageHidden?.Invoke();
                        });
        }
        
        private void SetupMessageContent(SORadioMessage message)
        {
                if (message.Sender)
                {
                        senderNameText.text = message.Sender.Name;
                        senderTitleText.text = message.Sender.Title;
                        if (message.Sender.Icon)
                        {
                                senderNameImage.gameObject.SetActive(true);
                                senderNameImage.sprite = message.Sender.Icon;
                        }
                        else
                        {
                                senderNameImage.gameObject.SetActive(false);
                        }
                }
                else
                {
                        senderNameImage.gameObject.SetActive(false);
                        senderNameText.text = "";
                        senderTitleText.text = "";
                }
                
                messageText.text = message.Message;
                messageWriter.RestartWriter();
                
                messageWriteSfx?.Play(audioSource);
        }
}