using System.Collections.Generic;
using UnityEngine;

namespace Components.Player.Helpers
{
    public sealed class Utils
    {
        public Material[] GetAllMaterials(GameObject go)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            List<Material> mats = new List<Material>();
            
            foreach (Renderer rend in rends)
            {
                mats.Add(rend.material);
            }

            return mats.ToArray();
        }
    }
}