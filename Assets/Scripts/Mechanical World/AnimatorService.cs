using System;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

namespace MechanicalWorld
{
    public sealed class AnimatorService : MonoBehaviour
    {
        [Header("Fade Materials")]
        [SerializeField] private Material hintMaterial;
        [SerializeField] private Material transitionMaterial;
        [SerializeField] private string colorPropertyName = "_Color";

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private float stayDuration = 0.1f;
        [SerializeField] private Ease fadeEase = Ease.InOutSine;

        [Header("PickUp Fly Animation")]
        [SerializeField] private string pickUpTargetTag = "PickUpMoveAnim";
        [SerializeField] private float pickUpSpeed = 10f;
        [SerializeField] private AnimationCurve pickUpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Mesh Rotation Settings")]
        [SerializeField] private float rotationDuration = 0.6f;

        private Color _hintInitialColor;
        private Color _transitionInitialColor;
        
        // В PrimeTween Sequence и Tween — это разные структуры
        private Tween _activeFadeTween;
        private Sequence _activeFadeSequence;
        
        private Transform _cachedParent;
        private Transform _cachedTargetPoint;

        private readonly Queue<RotationTask> _rotationQueue = new Queue<RotationTask>();
        private bool _isRotating = false;

        private struct RotationTask
        {
            public GameObject prefabInstance;
            public RotationAxis axis;
            public Action onComplete;
        }

        public bool IsFading => _activeFadeTween.isAlive || _activeFadeSequence.isAlive;

        private void Awake()
        {
            CachePickUpHierarchy();
            if (hintMaterial != null)
            {
                _hintInitialColor = hintMaterial.GetColor(colorPropertyName);
                SetAlpha(hintMaterial, 0f, _hintInitialColor);
            }
            if (transitionMaterial != null)
            {
                _transitionInitialColor = transitionMaterial.GetColor(colorPropertyName);
                SetAlpha(transitionMaterial, 0f, _transitionInitialColor);
            }
        }

        private void CachePickUpHierarchy()
        {
            GameObject rootObj = GameObject.FindWithTag(pickUpTargetTag);
            if (rootObj != null)
            {
                _cachedParent = rootObj.transform;
                _cachedTargetPoint = _cachedParent.childCount > 0 ? _cachedParent.GetChild(0) : _cachedParent;
            }
        }

        private void OnDestroy()
        {
            _activeFadeTween.Stop();
            _activeFadeSequence.Stop();
            Tween.StopAll(this);
            
            ResetMaterial(hintMaterial, _hintInitialColor);
            ResetMaterial(transitionMaterial, _transitionInitialColor);
        }

        public void PlayHintFade()
        {
            if (hintMaterial == null) return;
            
            _activeFadeTween.Stop();
            _activeFadeSequence.Stop();

            _activeFadeSequence = Sequence.Create()
                .Chain(FadeTo(hintMaterial, _hintInitialColor, 1f))
                .ChainDelay(stayDuration)
                .Chain(FadeTo(hintMaterial, _hintInitialColor, 0f));
        }

        public void PlayTransitionFade(Action onCameraSwitch)
        {
            if (transitionMaterial == null) { onCameraSwitch?.Invoke(); return; }
            
            _activeFadeTween.Stop();
            _activeFadeSequence.Stop();

            _activeFadeSequence = Sequence.Create()
                .Chain(FadeTo(transitionMaterial, _transitionInitialColor, 1f))
                .ChainCallback(() => onCameraSwitch?.Invoke())
                .ChainDelay(stayDuration)
                .Chain(FadeTo(transitionMaterial, _transitionInitialColor, 0f));
        }

        public void PlayPickUpAnimation(Transform visual, Action onComplete)
        {
            if (visual == null) { onComplete?.Invoke(); return; }
            if (_cachedTargetPoint == null) CachePickUpHierarchy();
            if (_cachedTargetPoint == null) { onComplete?.Invoke(); return; }

            visual.SetParent(_cachedTargetPoint, true);
            float distance = Vector3.Distance(visual.position, _cachedTargetPoint.position);
            float duration = distance / Mathf.Max(0.01f, pickUpSpeed);

            Tween.LocalPosition(visual, new Vector3(0, 0, visual.localPosition.z), duration, pickUpCurve)
                .OnComplete(() =>
                {
                    visual.gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
        }

        public void PlayMeshRotation(GameObject prefabInstance, RotationAxis axis, Action onComplete = null)
        {
            if (prefabInstance == null)
            {
                onComplete?.Invoke();
                return;
            }

            _rotationQueue.Enqueue(new RotationTask 
            { 
                prefabInstance = prefabInstance, 
                axis = axis, 
                onComplete = onComplete 
            });

            ProcessRotationQueue();
        }

        private void ProcessRotationQueue()
        {
            if (_isRotating || _rotationQueue.Count == 0) return;

            _isRotating = true;
            var task = _rotationQueue.Dequeue();

            if (task.prefabInstance == null)
            {
                FinishTask(task);
                return;
            }

            Transform meshTransform = task.prefabInstance.transform.Find("Mesh");
            if (meshTransform != null)
            {
                Vector3 rotationAmount = GetRotationVector(task.axis);
                Vector3 targetRotation = meshTransform.localEulerAngles + rotationAmount;

                Tween.LocalRotation(meshTransform, targetRotation, rotationDuration, Ease.Linear)
                    .OnComplete(() => FinishTask(task));
            }
            else
            {
                FinishTask(task);
            }
        }

        private void FinishTask(RotationTask task)
        {
            task.onComplete?.Invoke();
            _isRotating = false;
            ProcessRotationQueue();
        }

        private Vector3 GetRotationVector(RotationAxis axis)
        {
            return axis switch
            {
                RotationAxis.X => new Vector3(360, 0, 0),
                RotationAxis.Y => new Vector3(0, 360, 0),
                RotationAxis.Z => new Vector3(0, 0, 360),
                _ => Vector3.zero
            };
        }

        private Tween FadeTo(Material mat, Color initial, float targetAlpha)
        {
            float startAlpha = mat.GetColor(colorPropertyName).a;
            
            // В PrimeTween настройки Ease и прочего передаются прямо в метод Custom
            return Tween.Custom(
                startValue: startAlpha, 
                endValue: targetAlpha, 
                duration: fadeDuration, 
                onValueChange: a => SetAlpha(mat, a, initial),
                ease: fadeEase
            );
        }

        private void SetAlpha(Material mat, float alpha, Color initial) 
        { 
            if (mat != null) 
            { 
                Color c = initial; 
                c.a = alpha; 
                mat.SetColor(colorPropertyName, c); 
            } 
        }

        private void ResetMaterial(Material mat, Color initial) 
        { 
            if (mat != null) mat.SetColor(colorPropertyName, initial); 
        }
    }
}