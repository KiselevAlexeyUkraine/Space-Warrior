using UnityEngine;
using System.Collections;

namespace Components.Enemy
{
    public class EnemyShootLaser : EnemyBase
    {
        [Header("Laser Objects")]
        [Tooltip("Сам объект лазера (дочерний объект с рендерером и BoxCollider).")]
        [SerializeField] private GameObject _laserObject;
        
        private BoxCollider _laserCollider;
        private Renderer _laserRenderer;

        [Header("Laser Colors (Настройки цвета)")]
        [Tooltip("Цвет лазера в фазе подготовки (когда коллайдер еще выключен).")]
        [SerializeField] private Color _prepareColor = Color.white;
        
        [Tooltip("Цвет лазера в активной фазе (при расширении и жарке).")]
        [SerializeField] private Color _activeColor = Color.blue;

        [Header("Laser Timing (Настройки скорострельности)")]
        [Tooltip("Минимальное время отдыха между выстрелами лазера (в секундах).")]
        [SerializeField, Min(0.1f)] private float _minFireInterval = 3f;

        [Tooltip("Максимальное время отдыха между выстрелами лазера (в секундах).")]
        [SerializeField, Min(0.1f)] private float _maxFireInterval = 6f;

        [Tooltip("Задержка перед самым первым выстрелом после спавна врага.")]
        [SerializeField] private float _initialDelay = 1f;

        [Tooltip("Время ожидания ПОСЛЕ включения объекта, но ДО начала расширения (коллайдер выключен).")]
        [SerializeField, Min(0f)] private float _prepareDuration = 0.5f;

        [Tooltip("Время, за которое лазер вырастает от 0 до максимального масштаба.")]
        [SerializeField] private float _growDuration = 0.3f;

        [Tooltip("Продолжительность удержания лазера в максимальном масштабе (время жарки).")]
        [SerializeField] private float _activeDuration = 2f;

        [Tooltip("Время, за которое лазер сжимается обратно до 0.")]
        [SerializeField] private float _shrinkDuration = 0.2f;

        [Header("Laser Scale")]
        [Tooltip("Максимальный масштаб лазера по оси X, до которого он будет расширяться.")]
        [SerializeField] private float _targetScaleX = 2f;

        private Coroutine _laserRoutine;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (_laserObject != null)
            {
                _laserCollider = _laserObject.GetComponent<BoxCollider>();
                _laserRenderer = _laserObject.GetComponent<Renderer>();
                
                _laserObject.SetActive(false);
            }
            
            if (_laserCollider != null) _laserCollider.enabled = false;

            // Запускаем бесконечный цикл стрельбы лазером
            _laserRoutine = StartCoroutine(LaserAttackRoutine());
        }

        private void OnDisable()
        {
            if (_laserRoutine != null)
            {
                StopCoroutine(_laserRoutine);
                _laserRoutine = null;
            }
        }

        public override void Move()
        {
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;
        }

        /// <summary>
        /// Главный цикл работы лазера: Ожидание -> Включение (Белый) -> Рост & Коллайдер (Синий) -> Удержание -> Сжатие -> Отдых
        /// </summary>
        private IEnumerator LaserAttackRoutine()
        {
            // Самая первая задержка после появления врага
            yield return new WaitForSeconds(_initialDelay);

            while (true)
            {
                // --- ШАГ 1: АКТИВАЦИЯ И ОЖИДАНИЕ (ЦВЕТ: БЕЛЫЙ, КОЛЛАЙДЕР: ВЫКЛЮЧЕН) ---
                if (_laserObject != null)
                {
                    SetLaserXScale(0.05f); // Делаем тонкую полосочку для предупреждения
                    
                    if (_laserRenderer != null && _laserRenderer.material.HasProperty("_Color"))
                    {
                        _laserRenderer.material.color = _prepareColor;
                    }
                    
                    _laserObject.SetActive(true);
                }

                if (_laserCollider != null) _laserCollider.enabled = false; // Коллайдер строго выключен

                // Ждем время подготовки (время предупреждения)
                yield return new WaitForSeconds(_prepareDuration);


                // --- ШАГ 2: НАЧАЛО РАСШИРЕНИЯ (ЦВЕТ: СИНИЙ, КОЛЛАЙДЕР: ВКЛЮЧЕН) ---
                if (_laserRenderer != null && _laserRenderer.material.HasProperty("_Color"))
                {
                    _laserRenderer.material.color = _activeColor;
                }

                if (_laserCollider != null) _laserCollider.enabled = true; // Включаем коллайдер ИМЕННО СЕЙЧАС

                float elapsed = 0f;
                while (elapsed < _growDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / _growDuration;
                    SetLaserXScale(Mathf.Lerp(0.05f, _targetScaleX, progress));
                    yield return null;
                }
                SetLaserXScale(_targetScaleX);


                // --- ШАГ 3: ПРОДОЛЖИТЕЛЬНОСТЬ РАБОТЫ (УДЕРЖАНИЕ) ---
                yield return new WaitForSeconds(_activeDuration);


                // --- ШАГ 4: СЖАТИЕ ОБРАТНО ---
                elapsed = 0f;
                while (elapsed < _shrinkDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / _shrinkDuration;
                    SetLaserXScale(Mathf.Lerp(_targetScaleX, 0f, progress));
                    yield return null;
                }
                SetLaserXScale(0f);

                // Полностью выключаем физику и сам объект
                if (_laserCollider != null) _laserCollider.enabled = false;
                if (_laserObject != null) _laserObject.SetActive(false);


                // --- ШАГ 5: СЛУЧАЙНЫЙ ИНТЕРВАЛ ОТДЫХА (СКОРОСТРЕЛЬНОСТЬ) ---
                float min = Mathf.Min(_minFireInterval, _maxFireInterval);
                float max = Mathf.Max(_minFireInterval, _maxFireInterval);
                float randomCooldown = Random.Range(min, max);

                yield return new WaitForSeconds(randomCooldown);
            }
        }

        private void SetLaserXScale(float scaleX)
        {
            if (_laserObject == null) return;
            Vector3 currentScale = _laserObject.transform.localScale;
            currentScale.x = scaleX;
            _laserObject.transform.localScale = currentScale;
        }
    }
}