
using UnityEngine;
using VInspector;


public class Resource : MonoBehaviour
{

    [Header("Resource Settings")]
    [SerializeField, Min(0)] private float moveSpeed = 5f;
    [Tooltip("Time before the resource destroys itself (0 = unlimited time)"), SerializeField, Min(0)] private float lifetime = 10f;
    [SerializeField] private ResourceType resourceType;
    [SerializeField, Min(1), EnableIf("resourceType", ResourceType.Currency)] private int currencyWorth = 1;[EndIf]
    [SerializeField, Min(1), EnableIf("resourceType", ResourceType.HealthPack)] private int healthWorth = 1;[EndIf]
    [SerializeField, Min(1), EnableIf("resourceType", ResourceType.ShieldPack)] private int shieldWorth = 50;[EndIf]
    
    [Header("Collection Effects")]
    [SerializeField] private AudioClip collectionSound;
    [SerializeField] private ParticleSystem collectionEffect;

    
    
    
    private float _currentLifetime;
    private bool _isMagnetized;
    public ResourceType ResourceType => resourceType;
    public int HealthWorth => healthWorth;
    public int ShieldWorth => shieldWorth;
    public int CurrencyWorth => currencyWorth;


    private void Awake()
    {
        _currentLifetime = lifetime;
    }


    private void Update()
    {
        MoveAlongSpline();
        CheckLifetime();
    }
    

    #region State Management ---------------------------------------------------------------------------------------

    private void CheckLifetime()
    {
        if (lifetime <= 0f) return;

        _currentLifetime -= Time.deltaTime;
        if (_currentLifetime <= 0f)
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


        if (LevelManager.Instance && LevelManager.Instance.SplineContainer)
        {
            // Move along the spline
        }

    }
    
    
    public void MoveTowardsPlayer(Vector3 playerPosition, float speed)
    {
        Vector3 direction = (playerPosition - transform.position).normalized;
        transform.position += direction * (speed * Time.deltaTime);
    }
    
    

    #endregion Movement ---------------------------------------------------------------------------------------

    
    
    #region Resource Collection --------------------------------------------------------------------------------------

    public void ResourceCollected()
    {
        
        if (collectionSound)
        {
            AudioSource.PlayClipAtPoint(collectionSound, transform.position);
        }
        
        if (collectionEffect)
        {
            Instantiate(collectionEffect, transform.position, Quaternion.identity);
        }
        

        Destroy(gameObject);
    }
    

    #endregion Resource Collection --------------------------------------------------------------------------------------
    

}
