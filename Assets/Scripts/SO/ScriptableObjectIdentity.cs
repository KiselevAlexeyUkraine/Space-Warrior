using System;
using UnityEngine;

namespace SO
{
    public class ScriptableObjectIdentity : ScriptableObject
    {
        public string Id => _id;
        private string _id = Guid.NewGuid().ToString();
    }
}