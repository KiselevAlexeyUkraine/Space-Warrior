using UnityEngine.SceneManagement;

namespace Services
{
    public class SceneService
    {
        public int SceneToLoad { private get; set; } = DEFAULT_START_SCENE;

        private const int DEFAULT_START_SCENE = 2;
        
        public void Load()
        {
            SceneManager.LoadScene(SceneToLoad);
            SceneToLoad = DEFAULT_START_SCENE;
        }

        public void Load(int index)
        {
            SceneManager.LoadScene(index);
        }
        
        public void Load(string name)
        {
            SceneManager.LoadScene(name);
        }

        public void LoadNextScene()
        {
            var next = GetCurrentScene() + 1;

            if (next < SceneManager.sceneCountInBuildSettings)
            {
                Load(next);
            }
            else
            {
                Load(1);
            }
        }

        public int GetCurrentScene()
        {
            return SceneManager.GetActiveScene().buildIndex;
        }

        public int GetScenesCount()
        {
            return SceneManager.sceneCountInBuildSettings;
		}
    }
}
