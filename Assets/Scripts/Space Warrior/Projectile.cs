using UnityEngine;
using Components.Weapon;
using Components.Player;
using Level;
using Lean.Pool;
using Zenject;

public sealed class Projectile : MonoBehaviour 
{
    private Main _main; 
    private AudioService _audioService;

    [Header("Collision Settings")]
    [Tooltip("Слой (или слои), при столкновении с которыми пуля деспавнит цель и себя.")]
    [SerializeField] private LayerMask targetLayerMask;

    [Tooltip("Выберите слой ракеты (Rocket). Объекты на этом слое НЕ будут деспавниться при попадании пули.")]
    [SerializeField] private LayerMask _rocketLayer;

    [Header("Hit Effect")]
    [Tooltip("Префаб партиклов, который спавнится через LeanPool в точке столкновения при попадании по targetLayerMask.")]
    [SerializeField] private GameObject _hitEffectPrefab;

    [Header("Set Dynamically")]
    [SerializeField] private Rigidbody rigid;
    
    public Rigidbody Rigid => rigid;
    [SerializeField] private WeaponType _type;

    public WeaponType type
    {
        get => _type;
        set => SetType(value);
    }

    [Inject]
    private void Construct(Main main, AudioService audioService)
    {
        _main = main;
        _audioService = audioService;
    }
    

    private void OnTriggerEnter(Collider other)
    {
        GameObject otherGo = other.gameObject;
        int objectLayer = otherGo.layer;

        if (((1 << objectLayer) & targetLayerMask.value) != 0)
        {
            Debug.Log($"[Пуля] Столкновение с: {otherGo.name} на слое {LayerMask.LayerToName(objectLayer)}");

            SpawnHitEffect();
            PlayHitSound();

            if (((1 << objectLayer) & _rocketLayer.value) != 0)
            {
                Debug.Log($"[Пуля] Попадание в ракету. Объект {otherGo.name} игнорирует деспавн пули.");
            }
            else
            {
                SafeDespawn(otherGo);
            }
            
            SafeDespawn(gameObject);
        }
    }

    private void SpawnHitEffect()
    {
        if (_hitEffectPrefab == null)
            return;

        LeanPool.Spawn(_hitEffectPrefab, transform.position, transform.rotation);
    }

    private void PlayHitSound()
    {
        _audioService.PlayRandomEnemyHit();
    }

    private void SafeDespawn(GameObject go)
    {
        if (go == null) return;

        if (LeanPool.Links.ContainsKey(go))
        {
            LeanPool.Despawn(go);
        }
        else
        {
            Destroy(go);
        }
    }

    public void SetType(WeaponType eType)
    {
        _type = eType;
    }
}