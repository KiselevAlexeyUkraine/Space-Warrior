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

        public float Vertical =>
            (Input.GetKey(_config.Forward) ? 1f : 0f) +
            (Input.GetKey(_config.Backward) ? -1f : 0f);

        public float Horizontal =>
            (Input.GetKey(_config.Right) ? 1f : 0f) +
            (Input.GetKey(_config.Left) ? -1f : 0f);


        public bool Escape => Input.GetKeyDown(_config.Escape);
    }
}