using UnityEngine;

namespace Components.Helpers
{
	[DisallowMultipleComponent]
	public class PerObjectMaterialColor : MonoBehaviour
	{
		[field: SerializeField]
		public Color Color { set; private get; }

		private MaterialPropertyBlock _block;
		private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
	
		private void Awake()
		{
			OnValidate();
		}

		public void OnValidate()
		{
			_block ??= new MaterialPropertyBlock();
        
			_block.SetColor(ColorId, Color);
		
			GetComponentInChildren<Renderer>().SetPropertyBlock(_block);
		}
	}
}
