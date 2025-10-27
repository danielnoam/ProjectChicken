using System;
using UnityEngine;
using UnityEngine.UI;

public class ScrollingTexture : MonoBehaviour
{
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.5f, 0f);
    [SerializeField] private RawImage image;

    private Vector2 _defaultSize;
    private void Awake()
    {
     if (!image) return;
     
      _defaultSize = image.uvRect.size;
     
    }

    private void Update()
    {
        if (!image) return;
        
        Vector2 offset = scrollSpeed * Time.time;
        image.uvRect = new Rect(offset.x, offset.y, _defaultSize.x,_defaultSize.y);
    }
}