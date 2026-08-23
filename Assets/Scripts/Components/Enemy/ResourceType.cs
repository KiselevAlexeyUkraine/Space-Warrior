using System;
using UnityEngine;

namespace Components.Enemy
{
    
    public enum ResourceType
    {
        Iron,       // Железо
        Gold,       // Золото
        Crystals,   // Кристаллы
        Titanium    // Титан
    }
    [Serializable]
    public struct ResourceDrop
    {
        public ResourceType resourceType;
        [Min(0)] public int minAmount;
        [Min(0)] public int maxAmount;
    }

}