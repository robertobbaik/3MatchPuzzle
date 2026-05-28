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
        [SerializeField, Min(0.01f)] private float _minimumOrthographicSize = 1f;
        [SerializeField] private bool _fitOnStart = true;

        private void Start()
        {
            if (_fitOnStart)
            {
                FitToBoard();
            }
        }

        private void OnValidate()
        {
            _horizontalPaddingPixels = Mathf.Max(0f, _horizontalPaddingPixels);
            _minimumOrthographicSize = Mathf.Max(0.01f, _minimumOrthographicSize);
        }

        [ContextMenu("Fit To Board")]
        public void FitToBoard()
        {
            if (_targetCamera == null || _boardGrid == null)
            {
                Debug.LogWarning("BoardCameraFitter requires serialized Camera and BoardGrid references.", this);
                return;
            }

            Transform boardTransform = _boardGrid.transform;
            Vector3 boardScale = boardTransform.lossyScale;
            float boardWorldWidth = _boardGrid.Width * _boardGrid.CellSize * Mathf.Abs(boardScale.x);
            Vector3 boardLocalCenter = new Vector3(
                (_boardGrid.Width - 1) * _boardGrid.CellSize * 0.5f,
                (_boardGrid.Height - 1) * _boardGrid.CellSize * 0.5f,
                0f);
            Vector3 boardCenter = boardTransform.TransformPoint(boardLocalCenter);
            Vector3 cameraPosition = _targetCamera.transform.position;

            _targetCamera.orthographicSize = CalculateOrthographicSize(boardWorldWidth);
            _targetCamera.transform.position = new Vector3(boardCenter.x, boardCenter.y, cameraPosition.z);
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
