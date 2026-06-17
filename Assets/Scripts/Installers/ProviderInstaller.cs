using UnityEngine;
using Zenject;

namespace Installers
{
    public class ProviderInstaller<T> : MonoInstaller where T : ScriptableObject
    {
        public T[] Elements = new T[0];

        public override void InstallBindings()
        {
            for (int i = 0; i < Elements.Length; i++)
            {
                Container.Bind(Elements[i].GetType()).FromInstance(Elements[i]).AsSingle();
            }
        }
    }
}