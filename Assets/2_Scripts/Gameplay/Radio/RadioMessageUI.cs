using System;
using PrimeTween;
using TMPEffects.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RadioMessageUI : MonoBehaviour
{
        [Header("Settings")]
        [SerializeField] private float showDuration = 0.5f;
        [SerializeField] private float hideDuration = 0.5f;
        [SerializeField] private Ease ease = Ease.InOutQuart;
        [SerializeField] private Vector2 hiddenPosition = new Vector2(0,0);
        
        
        [Header("References")]
        [SerializeField] private TextMeshProUGUI senderNameText;
        [SerializeField] private Image senderNameImage;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TMPWriter messageWriter;
        
        private Sequence _hideSequence;
        private Sequence _showSequence;
        private Vector2 _shownPosition;
        private RectTransform _messageUIRectTransform;
        
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
                
                if (_showSequence.isAlive)
                {
                        _showSequence.Stop();
                        HideMessage();
                }
                
                _showSequence = Sequence.Create();
                if (_hideSequence.isAlive) _showSequence.ChainDelay(_hideSequence.duration);
                _showSequence.ChainCallback(() =>
                        {
                                if (message.Sender)
                                {
                                        senderNameText.text = message.Sender.Name;
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
                                }
                                
                                messageText.text = message.Message;
                                messageWriter.RestartWriter();
                        });
                _showSequence.Chain(Tween.UIAnchoredPosition(_messageUIRectTransform,hiddenPosition, _shownPosition, showDuration, ease));
                _showSequence.ChainDelay(message.Duration);
                _showSequence.OnComplete(() => OnMessageCompleted?.Invoke());
        }

        public void UpdateMessageOnly(SORadioMessage message)
        {
                if (!message) return;
                
                messageText.text = message.Message;
                messageWriter.RestartWriter();
                
                if (_showSequence.isAlive) _showSequence.Stop();
                _showSequence = Sequence.Create()
                        .ChainDelay(message.Duration)
                        .OnComplete(() => OnMessageCompleted?.Invoke()); 
        }

        public void HideMessage()
        {
                if (_hideSequence.isAlive) _hideSequence.Stop();
                if (_showSequence.isAlive) _showSequence.Stop();

                _hideSequence = Sequence.Create()
                        .Chain(Tween.UIAnchoredPosition(_messageUIRectTransform, hiddenPosition, hideDuration, ease))
                        .OnComplete(() => OnMessageHidden?.Invoke());
        }
}