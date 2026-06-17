using UnityEngine;
using Configs;
using Zenject;

namespace Services
{
    public class DesktopInput

    {
        private InputConfig _config;
        
        [Inject]
        private void Construct(InputConfig config)
        {
            _config = config;
        }

        // Движение
        public float Vertical =>
            (Input.GetKey(_config.Forward) ? 1f : 0f) +
            (Input.GetKey(_config.Backward) ? -1f : 0f);

        public float Horizontal =>
            (Input.GetKey(_config.Right) ? 1f : 0f) +
            (Input.GetKey(_config.Left) ? -1f : 0f);

        // Экшены
        public bool Jump => Input.GetKeyDown(_config.Jump);
        public bool Attack => Input.GetKeyDown(_config.Attack);
        public bool Interact => Input.GetKeyDown(_config.Interact);
        public bool Sprint => Input.GetKey(_config.Sprint);
        public bool Escape => Input.GetKeyDown(_config.Escape);
    }
}