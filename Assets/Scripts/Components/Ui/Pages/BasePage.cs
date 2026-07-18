using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Components.Ui.Pages
{
    [RequireComponent(typeof(CanvasGroup))]
    public class BasePage : MonoBehaviour
    {
        public event Action OnOpen;
        public event Action OnClose;
        public event Action Opened;
        public event Action Closed;

        // Новое событие с параметром
        public event Action<int> OnOpenWithParam;
        
        public PageName pageName;

        [Tooltip("Fade параметры для открытия: X - начальная прозрачность, Y - конечная прозрачность, Z - длительность в секундах")]
        [SerializeField] private Vector3 openFade = new(0f, 1f, 0.2f);

        [Tooltip("Fade параметры для закрытия: X - начальная прозрачность, Y - конечная прозрачность, Z - длительность в секундах")]
        [SerializeField] private Vector3 closeFade = new(1f, 0f, 0.2f);

        [SerializeField]
        private CanvasGroup _group;
        
        public PageSwitcher PageSwitcher { protected get; set; }

        private void Awake()
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
            }
            _group.alpha = 0f;
        }
        
        public async UniTask Open()
        {
            OnOpen?.Invoke();
            gameObject.SetActive(true);
            await Fade(openFade.x, openFade.y, openFade.z);
            
            // Проверка на случай, если объект уничтожили во время await Fade
            if (_group == null) return;
            
            Opened?.Invoke();
            _group.interactable = true;
        }

        // Новый метод с параметром
        public async UniTask Open(int reason)
        {
            OnOpen?.Invoke();
            OnOpenWithParam?.Invoke(reason);
            gameObject.SetActive(true);
            await Fade(openFade.x, openFade.y, openFade.z);
            
            // Проверка на случай, если объект уничтожили во время await Fade
            if (_group == null) return;
            
            Opened?.Invoke();
            _group.interactable = true;
        }
        
        public void OpenInstantly()
        {
            OnOpen?.Invoke();
            gameObject.SetActive(true);
            Opened?.Invoke();
            if (_group != null) _group.interactable = true;
        }

        public async UniTask Close()
        {
            if (_group != null) _group.interactable = false;
            OnClose?.Invoke();
            await Fade(closeFade.x, closeFade.y, closeFade.z);
            
            // Проверка на случай, если объект уничтожили во время await Fade
            if (gameObject == null) return;
            
            gameObject.SetActive(false);
            Closed?.Invoke();
        }
        
        public void CloseInstantly()
        {
            OnClose?.Invoke();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private async UniTask Fade(float start, float end, float duration)
        {
            if (_group == null) return;

            var elapsed = 0f;
            // Получаем токен отмены, привязанный к жизненному циклу этого GameObject
            var cancellationToken = this.GetCancellationTokenOnDestroy();
        
            while (elapsed <= duration)
            {
                // Если во время смены сцены/уровня CanvasGroup был уничтожен — выходим
                if (_group == null) return;

                elapsed += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, end, elapsed / duration);
                
                try
                {
                    // Передаем токен в ожидание следующего кадра. 
                    // Если объект уничтожится, UniTask прервет выполнение здесь.
                    await UniTask.NextFrame(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Ловим отмену при уничтожении объекта, чтобы предотвратить ошибку в консоли
                    return;
                }
            }

            // Финальный аккорд: выставляем точное значение, если компонент еще жив
            if (_group != null)
            {
                _group.alpha = end;
            }
        }
    }
}