using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RadioMessageUI : MonoBehaviour
{
        [Header("Settings")]
        [SerializeField] private float showDuration = 0.5f;
        [SerializeField] private float hideDuration = 0.5f;
        [SerializeField] private float visibleDuration = 3f;
        
        
        [Header("References")]
        [SerializeField] private TextMeshProUGUI senderNameText;
        [SerializeField] private Image senderNameImage;
        [SerializeField] private TextMeshProUGUI messageText;
        
        private Sequence _hideSequence;
        private Sequence _showSequence;
        private Vector2 _messageUISize;
        private RectTransform _messageUIRectTransform;

        private void Awake()
        {
                _messageUIRectTransform = (RectTransform)transform;
                _messageUISize = _messageUIRectTransform.sizeDelta;
                _messageUIRectTransform.sizeDelta = new Vector2(-_messageUISize.x*2, 0);
        }


        public void ShowMessage(SORadioMessage message)
        {
                if (_showSequence.isAlive)
                {
                        _showSequence.Stop();
                        HideMessage();
                }
                
                _showSequence = Sequence.Create();
                if (_hideSequence.isAlive) _showSequence.ChainDelay(_hideSequence.duration);
                _showSequence.ChainCallback((() =>
                        {
                                senderNameText.text = message.Sender.Name;
                                senderNameImage.sprite = message.Sender.Icon;
                                messageText.text = message.Message;
                        }));
                _showSequence.Chain(Tween.UIAnchoredPosition(_messageUIRectTransform,new Vector2(-_messageUISize.x*2,0), Vector2.zero, showDuration, Ease.InOutQuart));
                _showSequence.ChainDelay(visibleDuration);
                _showSequence.OnComplete(HideMessage);

        }

        public void HideMessage()
        {
                if (_hideSequence.isAlive) _hideSequence.Stop();
                if (_showSequence.isAlive) _showSequence.Stop();
                
                
                _hideSequence = Sequence.Create();
                _hideSequence.Chain(Tween.UIAnchoredPosition(_messageUIRectTransform, new Vector2(-_messageUISize.x*2,0), hideDuration, Ease.InOutQuart));
        }
        
}