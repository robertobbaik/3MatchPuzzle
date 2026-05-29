using ThreeMatch.Board;
using UnityEngine;

namespace ThreeMatch.View
{
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCameraFitter : MonoBehaviour
    {
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private BoardGrid _boardGrid;
        [SerializeField, Min(0f)] private float _horizontalPaddingPixels = 48f;
        [SerializeField, Min(0.01f)] private float _minimumOrthographicSize = 10f;

        public void Initialize(Camera targetCamera, BoardGrid boardGrid)
        {
            _targetCamera = targetCamera;
            _boardGrid = boardGrid;
            NormalizeSettings();
        }

        private void NormalizeSettings()
        {
            _horizontalPaddingPixels = Mathf.Max(0f, _horizontalPaddingPixels);
            _minimumOrthographicSize = Mathf.Max(0.01f, _minimumOrthographicSize);
        }

        [ContextMenu("Fit To Board")]
        public void FitToBoard()
        {
            NormalizeSettings();

            Transform boardTransform = _boardGrid.transform;
            Vector3 boardScale = boardTransform.lossyScale;
            float boardWorldWidth = _boardGrid.Width * _boardGrid.CellSize * Mathf.Abs(boardScale.x);

            _targetCamera.orthographicSize = CalculateOrthographicSize(boardWorldWidth);
        }

        private float CalculateOrthographicSize(float boardWorldWidth)
        {
            int screenWidth = Mathf.Max(1, Screen.width);
            int screenHeight = Mathf.Max(1, Screen.height);
            float usablePixelWidth = Mathf.Max(1f, screenWidth - _horizontalPaddingPixels * 2f);
            float visibleWorldWidth = boardWorldWidth * screenWidth / usablePixelWidth;
            float screenAspect = (float)screenWidth / screenHeight;
            float orthographicSize = visibleWorldWidth / screenAspect * 0.5f;

            return Mathf.Max(_minimumOrthographicSize, orthographicSize);
        }
    }
}
