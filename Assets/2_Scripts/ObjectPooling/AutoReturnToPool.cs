using System;
using UnityEngine;

public class AutoReturnToPool : MonoBehaviour
{
        
        
        private float _lifeTime;
        private bool _isInitialized;


        private void Update()
        {
                if (!_isInitialized) return;
                
                CheckLiftTime();
        }
        
        
        private void CheckLiftTime()
        {
                _lifeTime -= Time.deltaTime;
                if (_lifeTime <= 0f)
                {
                        ReturnToPool();
                }
        }
        
        private void ReturnToPool()
        {
                _isInitialized = false;
                ObjectPooler.ReturnObjectToPool(gameObject);
        }
        
        public void Initialize(float lifeTime)
        {
                _lifeTime = lifeTime;
                _isInitialized = true;
        }

}