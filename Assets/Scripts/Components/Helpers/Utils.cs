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
                // Add all materials from the renderer (handles multi-material renderers)
                if (rend == null) continue;
                var rendMats = rend.materials; // returns all instance materials
                if (rendMats != null && rendMats.Length > 0)
                {
                    mats.AddRange(rendMats);
                }
            }

            return mats.ToArray();
        }
    }
}