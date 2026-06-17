using Services;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

public class ButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
	[SerializeField] private AudioClip _hover;
	[SerializeField] private AudioClip _click;

	private AudioService _audio;

	[Inject]
	private void Construct(AudioService audioService)
	{
		_audio = audioService;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		_audio.PlayOneShot(_click);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_audio.PlayOneShot(_hover);
	}
}
