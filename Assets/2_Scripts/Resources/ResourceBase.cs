using System;
using UnityEngine;


public abstract class ResourceBase : MonoBehaviour
{

    [Header("Resource Settings")]
    [SerializeField, Min(0)] private float moveSpeed = 5f;
    [SerializeField, Min(0)] private float lifetime = 10f;
    
    [Header("Collection Effects")]
    [SerializeField] private AudioClip collectionSound;
    [SerializeField] private ParticleSystem collectionEffect;
    
    private bool _isMagnetized;
    
    private void Update()
    {
        MoveAlongSpline();
        CheckLifetime();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out RailPlayer player))
        {
            ResourceCollected(player);
        }
    }

    #region State Management ---------------------------------------------------------------------------------------

    private void CheckLifetime()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void SetMagnetized(bool magnetized)
    {
        _isMagnetized = magnetized;
    }

    #endregion State Management ---------------------------------------------------------------------------------------
     

    
    

    #region Movement ---------------------------------------------------------------------------------------

    private void MoveAlongSpline()
    {
        if (!_isMagnetized) return;
            


    }
    
    
    public void MoveTowardsPlayer(Vector3 playerPosition)
    {
        Vector3 direction = (playerPosition - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }
    
    

    #endregion Movement ---------------------------------------------------------------------------------------

    
    
    #region Resource Collection --------------------------------------------------------------------------------------

    private void ResourceCollected(RailPlayer player)
    {
        Debug.Log($"{gameObject.name} collected by player!");
        
        if (collectionSound)
        {
            AudioSource.PlayClipAtPoint(collectionSound, transform.position);
        }
        
        if (collectionEffect)
        {
            Instantiate(collectionEffect, transform.position, Quaternion.identity);
        }
        
        UpdatePlayerResourceCollected(player);

        Destroy(gameObject);
    }
    

    protected abstract void UpdatePlayerResourceCollected(RailPlayer player);

    #endregion Resource Collection --------------------------------------------------------------------------------------
    

}
