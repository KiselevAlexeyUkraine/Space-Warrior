using UnityEngine;
using Configs;
using Zenject;

namespace Components.Player
{
    public class DesktopInput
    {
        private InputConfig _config;
        
        [Inject]
        private void Construct(InputConfig config)
        {
            _config = config;
        }

        // Проверяем (W или Стрелка Вверх) и (S или Стрелка Вниз)
        public float Vertical =>
            (Input.GetKey(_config.Forward) || Input.GetKey(_config.ArrowUp) ? 1f : 0f) +
            (Input.GetKey(_config.Backward) || Input.GetKey(_config.ArrowDown) ? -1f : 0f);

        // Проверяем (D или Стрелка Вправо) и (A или Стрелка Влево)
        public float Horizontal =>
            (Input.GetKey(_config.Right) || Input.GetKey(_config.ArrowRight) ? 1f : 0f) +
            (Input.GetKey(_config.Left) || Input.GetKey(_config.ArrowLeft) ? -1f : 0f);

        public bool Escape => Input.GetKeyDown(_config.Escape);
    }
}