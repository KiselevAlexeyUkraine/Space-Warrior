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

        // новое событие с параметром
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
            _group.alpha = 0f;
        }
        
        public async UniTask Open()
        {
            OnOpen?.Invoke();
            gameObject.SetActive(true);
            await Fade(openFade.x, openFade.y, openFade.z);
            Opened?.Invoke();
            _group.interactable = true;
        }

        // новый метод с параметром
        public async UniTask Open(int reason)
        {
            OnOpen?.Invoke();
            OnOpenWithParam?.Invoke(reason);
            gameObject.SetActive(true);
            await Fade(openFade.x, openFade.y, openFade.z);
            Opened?.Invoke();
            _group.interactable = true;
        }
        
        public void OpenInstantly()
        {
            OnOpen?.Invoke();
            gameObject.SetActive(true);
            Opened?.Invoke();
        }

        public async UniTask Close()
        {
            _group.interactable = false;
            OnClose?.Invoke();
            await Fade(closeFade.x, closeFade.y, closeFade.z);
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
            var elapsed = 0f;
        
            while (elapsed <= duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, end, elapsed / duration);
                await UniTask.NextFrame();
            }
        }
    }
}
