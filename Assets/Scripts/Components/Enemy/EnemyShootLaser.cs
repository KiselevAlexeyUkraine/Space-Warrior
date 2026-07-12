using UnityEngine;
using System.Collections;
using Zenject;
using Components.Player;

namespace Components.Enemy
{
    public class EnemyShootLaser : EnemyBase
    {
        [Header("Laser Objects")]
        [Tooltip("Объект предупреждения перед выстрелом (включается на время подготовки, коллайдера нет).")]
        [SerializeField] private GameObject _prepareObject;

        [Tooltip("Объект самого лазера (содержит BoxCollider, растягивается по X во время выстрела).")]
        [SerializeField] private GameObject _activeObject;

        private BoxCollider _laserCollider;

        private AudioService _audioService;

        [Inject]
        private void Construct(AudioService audioService)
        {
            _audioService = audioService;
        }

        [Header("Laser Timing (Настройки скорострельности)")]
        [Tooltip("Минимальное время отдыха между выстрелами лазера (в секундах).")]
        [SerializeField, Min(0.1f)] private float _minFireInterval = 3f;

        [Tooltip("Максимальное время отдыха между выстрелами лазера (в секундах).")]
        [SerializeField, Min(0.1f)] private float _maxFireInterval = 6f;

        [Tooltip("Минимальная задержка перед самым первым выстрелом после спавна врага.")]
        [SerializeField, Min(0f)] private float _minInitialDelay = 1f;

        [Tooltip("Максимальная задержка перед самым первым выстрелом после спавна врага.")]
        [SerializeField, Min(0f)] private float _maxInitialDelay = 1f;

        [Tooltip("Время ожидания ПОСЛЕ включения объекта подготовки, ДО начала выстрела (коллайдер выключен).")]
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

            if (_activeObject != null)
            {
                _laserCollider = _activeObject.GetComponent<BoxCollider>();
                _activeObject.SetActive(false);
            }

            if (_laserCollider != null) _laserCollider.enabled = false;

            if (_prepareObject != null) _prepareObject.SetActive(false);

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
        /// Главный цикл работы лазера: Ожидание -> Подготовка (PrepareObject) -> Рост & Коллайдер (ActiveObject) -> Удержание -> Сжатие -> Отдых
        /// </summary>
        private IEnumerator LaserAttackRoutine()
        {
            // Самая первая задержка после появления врага (случайная в диапазоне min/max)
            float minInitial = Mathf.Min(_minInitialDelay, _maxInitialDelay);
            float maxInitial = Mathf.Max(_minInitialDelay, _maxInitialDelay);
            float randomInitialDelay = Random.Range(minInitial, maxInitial);

            yield return new WaitForSeconds(randomInitialDelay);

            while (true)
            {
                // --- ШАГ 1: ПОДГОТОВКА К ВЫСТРЕЛУ (КОЛЛАЙДЕР: ВЫКЛЮЧЕН) ---
                if (_prepareObject != null) _prepareObject.SetActive(true);

                if (_laserCollider != null) _laserCollider.enabled = false; // Коллайдер строго выключен

                if (_audioService != null) _audioService.PlayRandomLaserPrepare();

                // Ждем время подготовки (время предупреждения)
                yield return new WaitForSeconds(_prepareDuration);

                if (_prepareObject != null) _prepareObject.SetActive(false);


                // --- ШАГ 2: НАЧАЛО ВЫСТРЕЛА (КОЛЛАЙДЕР: ВКЛЮЧЕН) ---
                if (_activeObject != null)
                {
                    SetLaserXScale(0.05f); // Стартуем с тонкой полоски
                    _activeObject.SetActive(true);
                }

                if (_laserCollider != null) _laserCollider.enabled = true; // Включаем коллайдер ИМЕННО СЕЙЧАС

                if (_audioService != null) _audioService.PlayRandomLaserShoot();

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
                if (_activeObject != null) _activeObject.SetActive(false);


                // --- ШАГ 5: СЛУЧАЙНЫЙ ИНТЕРВАЛ ОТДЫХА (СКОРОСТРЕЛЬНОСТЬ) ---
                float min = Mathf.Min(_minFireInterval, _maxFireInterval);
                float max = Mathf.Max(_minFireInterval, _maxFireInterval);
                float randomCooldown = Random.Range(min, max);

                yield return new WaitForSeconds(randomCooldown);
            }
        }

        private void SetLaserXScale(float scaleX)
        {
            if (_activeObject == null) return;
            Vector3 currentScale = _activeObject.transform.localScale;
            currentScale.x = scaleX;
            _activeObject.transform.localScale = currentScale;
        }
    }
}