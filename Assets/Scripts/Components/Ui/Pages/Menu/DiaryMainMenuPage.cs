using UnityEngine;
using UnityEngine.UI;

namespace Components.Ui.Pages.Menu
{
    public class DiaryMainMenuPage : BasePage
    {
        [Header("Note Slots (Default Images)")]
        [SerializeField] private Image[] _noteImages; // Ожидается 2 изображения
        [SerializeField] private Sprite _defaultSprite;

        [Header("Collected Note Sprites")]
        [SerializeField] private Sprite[] _collectedNoteSprites;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _exitButton;

        [Header("Reset Button")]
        [SerializeField] private Button _resetButton;

        private int _currentPairIndex = 0;

        private void Awake()
        {
            _prevButton.onClick.AddListener(ShowPreviousPair);
            _nextButton.onClick.AddListener(ShowNextPair);
            _exitButton.onClick.AddListener(() => { PageSwitcher.Open(PageName.Menu).Forget(); });
            _resetButton.onClick.AddListener(ResetAllNotes);
        }

        private void OnEnable()
        {
            LoadNoteSprites();
            ShowNotePairAt(_currentPairIndex);
        }

        private void LoadNoteSprites()
        {
            for (int i = 0; i < _collectedNoteSprites.Length && i < _noteImages.Length; i++)
            {
                bool isCollected = PlayerPrefs.GetInt($"Note_{i}", 0) == 1;

                _noteImages[i].sprite = isCollected
                    ? _collectedNoteSprites[i]
                    : _defaultSprite;

                _noteImages[i].gameObject.SetActive(false);
            }
        }

        private void ShowNotePairAt(int pairIndex)
        {
            for (int i = 0; i < _noteImages.Length; i++)
            {
                int noteIndex = pairIndex * _noteImages.Length + i;

                if (noteIndex < _collectedNoteSprites.Length)
                {
                    bool isCollected = PlayerPrefs.GetInt($"Note_{noteIndex}", 0) == 1;

                    _noteImages[i].sprite = isCollected
                        ? _collectedNoteSprites[noteIndex]
                        : _defaultSprite;

                    _noteImages[i].gameObject.SetActive(true);
                }
                else
                {
                    _noteImages[i].gameObject.SetActive(false);
                }
            }

            int maxPair = Mathf.CeilToInt(_collectedNoteSprites.Length / (float)_noteImages.Length) - 1;
            _prevButton.interactable = pairIndex > 0;
            _nextButton.interactable = pairIndex < maxPair;
        }

        private void ShowPreviousPair()
        {
            if (_currentPairIndex > 0)
            {
                _currentPairIndex--;
                ShowNotePairAt(_currentPairIndex);
            }
        }

        private void ShowNextPair()
        {
            int maxPair = Mathf.CeilToInt(_collectedNoteSprites.Length / (float)_noteImages.Length) - 1;

            if (_currentPairIndex < maxPair)
            {
                _currentPairIndex++;
                ShowNotePairAt(_currentPairIndex);
            }
        }

        private void ResetAllNotes()
        {
            for (int i = 0; i < _collectedNoteSprites.Length; i++)
            {
                PlayerPrefs.DeleteKey($"Note_{i}");
            }

            PlayerPrefs.Save();
            _currentPairIndex = 0;
            LoadNoteSprites();
            ShowNotePairAt(_currentPairIndex);
        }

        private void OnDestroy()
        {
            _prevButton.onClick.RemoveAllListeners();
            _nextButton.onClick.RemoveAllListeners();
            _exitButton.onClick.RemoveAllListeners();
            _resetButton.onClick.RemoveAllListeners();
        }
    }
}
