using UnityEngine;
using UnityEngine.UI;

namespace Components.Ui.Elements
{
    [RequireComponent(typeof(Button))]
    public class LinkButton : MonoBehaviour
    {
        private Button _button;
     
        public string Url { get; set; }
        
        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void Start()
        {
            _button.onClick.AddListener(() => Application.OpenURL(Url));
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
        }
    }
}