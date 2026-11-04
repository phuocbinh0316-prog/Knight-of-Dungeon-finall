using UnityEngine;

namespace Knight
{
    /// <summary>
    /// Smooth parallax scrolling layer compatible with Cinemachine and Orthographic 2D camera.
    /// Supports configurable horizontal and vertical parallax factors and infinite horizontal looping.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(100)] // Executes after Cinemachine updates the camera position
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Target Camera")]
        [Tooltip("Camera to track. If null, Camera.main will be automatically used.")]
        [SerializeField] private Transform targetCamera;

        [Header("Parallax Settings")]
        [Tooltip("Parallax multiplier (1 = follows camera 100%, 0 = static in world, 0.9 = far background).")]
        [SerializeField] private Vector2 parallaxMultiplier = new Vector2(0.8f, 0.2f);

        [Header("Infinite Tiling / Looping")]
        [Tooltip("Automatically wrap horizontally when camera moves beyond texture unit size.")]
        [SerializeField] private bool infiniteHorizontal = true;

        [Tooltip("Width of one repeating segment in world units (pixels / PPU).")]
        [SerializeField] private float textureUnitSizeX = 32f;

        private Vector3 startPosition;
        private Vector3 startCameraPosition;
        private bool isInitialized = false;

        public Transform TargetCamera
        {
            get => targetCamera;
            set => targetCamera = value;
        }

        public Vector2 ParallaxMultiplier
        {
            get => parallaxMultiplier;
            set => parallaxMultiplier = value;
        }

        public bool InfiniteHorizontal
        {
            get => infiniteHorizontal;
            set => infiniteHorizontal = value;
        }

        public float TextureUnitSizeX
        {
            get => textureUnitSizeX;
            set => textureUnitSizeX = value;
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (targetCamera == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    targetCamera = mainCam.transform;
                }
            }

            startPosition = transform.position;
            if (targetCamera != null)
            {
                startCameraPosition = targetCamera.position;
                isInitialized = true;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    targetCamera = mainCam.transform;
                    if (!isInitialized)
                    {
                        startCameraPosition = targetCamera.position;
                        startPosition = transform.position;
                        isInitialized = true;
                    }
                }
                else
                {
                    return;
                }
            }

            Vector3 camDelta = targetCamera.position - startCameraPosition;
            Vector3 targetPos = startPosition + new Vector3(
                camDelta.x * parallaxMultiplier.x,
                camDelta.y * parallaxMultiplier.y,
                0f
            );

            if (infiniteHorizontal && textureUnitSizeX > 0.1f)
            {
                float offsetFromCamera = targetCamera.position.x - targetPos.x;
                while (offsetFromCamera >= textureUnitSizeX)
                {
                    startPosition.x += textureUnitSizeX;
                    targetPos.x += textureUnitSizeX;
                    offsetFromCamera -= textureUnitSizeX;
                }
                while (offsetFromCamera <= -textureUnitSizeX)
                {
                    startPosition.x -= textureUnitSizeX;
                    targetPos.x -= textureUnitSizeX;
                    offsetFromCamera += textureUnitSizeX;
                }
            }

            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        }
    }
}
