using Components.Player;
using UnityEngine;
using Zenject;

namespace Components.Helpers
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField]
        private string _sceneName;

        [SerializeField]
        private float _delayInSeconds = 0f;

        private SceneService _sceneService;

        [Inject]
        private void Construct(SceneService sceneService)
        {
            _sceneService = sceneService;
        }

        private void Start()
        {
            StartCoroutine(LoadSceneAfterDelay());
        }

        private System.Collections.IEnumerator LoadSceneAfterDelay()
        {
            if (_delayInSeconds > 0f)
                yield return new WaitForSeconds(_delayInSeconds);

            _sceneService.Load(_sceneName);
        }
    }
}