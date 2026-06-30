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

        [Header("Laser Timing (Настройки скорострельности)")]
        [Tooltip("Минимальное время отдыха между выстрелами лазера (в секундах).")]
        [SerializeField, Min(0.1f)] private float _minFireInterval = 3f;

        [Tooltip("Максимальное время отдыха между выстрелами лазера (в секундах).")]
        [SerializeField, Min(0.1f)] private float _maxFireInterval = 6f;

        [Tooltip("Задержка перед самым первым выстрелом после спавна врага.")]
        [SerializeField] private float _initialDelay = 1f;

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
            _laserCollider = _laserObject.GetComponent<BoxCollider>();
            // Принудительно выключаем лазер и его коллайдер при старте/возврате в пул
            if (_laserObject != null) _laserObject.SetActive(false);
            if (_laserCollider != null) _laserCollider.enabled = false;

            // Запускаем бесконечный цикл стрельбы лазером
            _laserRoutine = StartCoroutine(LaserAttackRoutine());
        }

        private void OnDisable()
        {
            // Обязательно останавливаем корутину, если враг деспавнится во время выстрела
            if (_laserRoutine != null)
            {
                StopCoroutine(_laserRoutine);
                _laserRoutine = null;
            }
        }

        public override void Move()
        {
            // Стандартное движение вниз (если не Grid Formation)
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;
        }

        /// <summary>
        /// Главный цикл работы лазера: Ожидание -> Рост -> Удержание -> Сжатие -> Отдых
        /// </summary>
        private IEnumerator LaserAttackRoutine()
        {
            // Самая первая задержка после появления врага
            yield return new WaitForSeconds(_initialDelay);

            while (true)
            {
                // --- ШАГ 1: АКТИВАЦИЯ И РОСТ ---
                if (_laserObject != null)
                {
                    Vector3 initialScale = _laserObject.transform.localScale;
                    initialScale.x = 0f; // Начинаем с нуля по X
                    _laserObject.transform.localScale = initialScale;
                    _laserObject.SetActive(true);
                }

                // Включаем коллайдер в момент начала расширения
                if (_laserCollider != null) _laserCollider.enabled = true;

                float elapsed = 0f;
                while (elapsed < _growDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / _growDuration;
                    SetLaserXScale(Mathf.Lerp(0f, _targetScaleX, progress));
                    yield return null;
                }
                SetLaserXScale(_targetScaleX); // На всякий случай фиксируем точное значение

                // --- ШАГ 2: ПРОДОЛЖИТЕЛЬНОСТЬ РАБОТЫ (УДЕРЖАНИЕ) ---
                yield return new WaitForSeconds(_activeDuration);

                // --- ШАГ 3: СЖАТИЕ ОБРАТНО ---
                elapsed = 0f;
                while (elapsed < _shrinkDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / _shrinkDuration;
                    SetLaserXScale(Mathf.Lerp(_targetScaleX, 0f, progress));
                    yield return null;
                }
                SetLaserXScale(0f);

                // Выключаем коллайдер и сам объект, так как выстрел окончен
                if (_laserCollider != null) _laserCollider.enabled = false;
                if (_laserObject != null) _laserObject.SetActive(false);

                // --- ШАГ 4: СЛУЧАЙНЫЙ ИНТЕРВАЛ ОТДЫХА (СКОРОСТРЕЛЬНОСТЬ) ---
                float min = Mathf.Min(_minFireInterval, _maxFireInterval);
                float max = Mathf.Max(_minFireInterval, _maxFireInterval);
                float randomCooldown = Random.Range(min, max);

                yield return new WaitForSeconds(randomCooldown);
            }
        }

        /// <summary>
        /// Вспомогательный метод для изменения масштаба только по оси X
        /// </summary>
        private void SetLaserXScale(float scaleX)
        {
            if (_laserObject == null) return;
            Vector3 currentScale = _laserObject.transform.localScale;
            currentScale.x = scaleX;
            _laserObject.transform.localScale = currentScale;
        }
    }
}