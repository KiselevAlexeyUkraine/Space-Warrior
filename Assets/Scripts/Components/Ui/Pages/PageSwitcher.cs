using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Components.Ui.Pages
{
    public class PageSwitcher : MonoBehaviour
    {
        [SerializeField] private PageName _startPage;
        [SerializeField] private List<BasePage> _pages = new();

        private BasePage _currentPage;

        private void Awake()
        {
            foreach (var page in _pages)
            {
                page.PageSwitcher = this;
            }
            
            foreach (var page in _pages)
            {
                if (page.pageName != _startPage)
                {
                    page.CloseInstantly();
                    continue;
                }
                
                _currentPage = page;
                _currentPage.Open().Forget();
            }
        }

        public async UniTaskVoid Open(PageName pageName)
        {
            for (var i = 0; i < _pages.Count; i++)
            {
                if (_pages[i].pageName == pageName)
                {
                    await _currentPage.Close();
                    _currentPage = _pages[i];
                    await _currentPage.Open();
                    return;
                }
            }
        }
        
        public async UniTaskVoid Open(PageName pageName, int param)
        {
            for (var i = 0; i < _pages.Count; i++)
            {
                if (_pages[i].pageName == pageName)
                {
                    await _currentPage.Close();
                    _currentPage = _pages[i];
                    await _currentPage.Open(param);
                    return;
                }
            }
        }
    }
}
