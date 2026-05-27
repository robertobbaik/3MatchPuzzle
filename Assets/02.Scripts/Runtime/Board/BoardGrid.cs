using System.Collections.Generic;
using UnityEngine;

namespace ThreeMatch.Board
{
    public sealed class BoardGrid : MonoBehaviour
    {
        private const int MinBoardSize = 1;

        [Header("Board Size")]
        [SerializeField, Min(MinBoardSize)] private int _width = 8;
        [SerializeField, Min(MinBoardSize)] private int _height = 8;

        [Header("Layout")]
        [SerializeField, Min(0.1f)] private float _cellSize = 1f;

        [Header("View")]
        [SerializeField] private Transform _cellRoot;
        [SerializeField] private BoardCellView _cellPrefab;
        [SerializeField] private BoardInputController _inputController;
        [SerializeField] private bool _buildOnStart = true;

        private readonly List<BoardCellView> _cells = new List<BoardCellView>();

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public IReadOnlyList<BoardCellView> Cells => _cells;

        private void Start()
        {
            if (_buildOnStart)
            {
                RebuildBoard();
            }
        }

        private void OnValidate()
        {
            _width = Mathf.Max(MinBoardSize, _width);
            _height = Mathf.Max(MinBoardSize, _height);
            _cellSize = Mathf.Max(0.1f, _cellSize);
            ResolveInputController();
        }

        [ContextMenu("Rebuild Board")]
        public void RebuildBoard()
        {
            if (_cellPrefab == null)
            {
                Debug.LogError("BoardGrid requires a cell prefab before rebuilding the board.", this);
                return;
            }

            Transform root = ResolveCellRoot();

            ClearGeneratedCells(root);
            _cells.Clear();

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    BoardCellView cell = CreateCell(root, x, y);
                    _cells.Add(cell);
                }
            }
        }

        private Transform ResolveCellRoot()
        {
            if (_cellRoot != null)
            {
                return _cellRoot;
            }

            return transform;
        }

        private BoardInputController ResolveInputController()
        {
            if (_inputController != null)
            {
                return _inputController;
            }

            _inputController = GetComponent<BoardInputController>();
            return _inputController;
        }

        private void ClearGeneratedCells(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);

                if (!child.TryGetComponent(out BoardCellView _))
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private BoardCellView CreateCell(Transform root, int x, int y)
        {
            BoardCellView cell = Instantiate(_cellPrefab, root);
            GameObject cellObject = cell.gameObject;

            cellObject.transform.SetParent(root, false);
            cellObject.transform.localPosition = GetCellPosition(x, y);
            cellObject.transform.localRotation = Quaternion.identity;
            cellObject.transform.localScale = Vector3.one * (_cellSize * 0.95f);

            cell.Configure(x, y, ResolveInputController());

            return cell;
        }

        private Vector3 GetCellPosition(int x, int y)
        {
            float positionX = x * _cellSize;
            float positionY = y * _cellSize;

            return new Vector3(positionX, positionY, 0f);
        }
    }
}
