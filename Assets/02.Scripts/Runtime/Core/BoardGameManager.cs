using ThreeMatch.Board;
using ThreeMatch.View;
using UnityEngine;

namespace ThreeMatch.Core
{
    public sealed class BoardGameManager : MonoBehaviour
    {
        [SerializeField] private BoardGrid _boardGrid;
        [SerializeField] private BoardInputController _inputController;
        [SerializeField] private BoardCameraFitter _cameraFitter;
        [SerializeField] private Camera _gameCamera;
        [SerializeField] private bool _buildBoardOnStart = true;
        [SerializeField] private bool _fitCameraOnStart = true;

        private void Awake()
        {
            _boardGrid.Initialize(_inputController);
            _inputController.Initialize(_boardGrid, _gameCamera);
            _cameraFitter.Initialize(_gameCamera, _boardGrid);
        }

        private void OnEnable()
        {
            _inputController.EnableInput();
        }

        private void Start()
        {
            if (_buildBoardOnStart)
            {
                _boardGrid.RebuildBoard();
            }

            if (_fitCameraOnStart)
            {
                _cameraFitter.FitToBoard();
            }
        }

        private void OnDisable()
        {
            _inputController.DisableInput();
        }

        private void OnDestroy()
        {
            _inputController.DisposeInput();
        }
    }
}
