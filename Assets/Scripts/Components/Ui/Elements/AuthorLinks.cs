using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Components.Ui.Elements
{
    [Serializable]
    public struct Link
    {
        public LinkButton LinkButton;
        public string Url;
    }
    
    public class AuthorLinks : MonoBehaviour
    {
        [SerializeField] private string _name;
        [SerializeField] private string _role;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _roleText;
        [SerializeField] private List<Link> _linkButtons;

        private void Awake()
        {
            OnValidate();
        }
        
        private void OnValidate()
        {
            _nameText.text = _name;
            _roleText.text = _role;

            foreach (var element in _linkButtons)
            {
                if (string.IsNullOrEmpty(element.Url))
                {
                    element.LinkButton.gameObject.SetActive(false);
                }
                else
                {
                    element.LinkButton.gameObject.SetActive(true);
                    element.LinkButton.Url = element.Url;
                }
            }
        }
    }
}