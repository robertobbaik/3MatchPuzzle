using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThreeMatch.Tiles;
#if DOTWEEN
using DG.Tweening;
#endif

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
        [SerializeField] private Transform _poolRoot;
        [SerializeField] private List<BoardCellView> _defaultBlockPrefabs = new List<BoardCellView>();
        [SerializeField] private BoardInputController _inputController;
        [SerializeField] private bool _buildOnStart = true;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float _swapDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float _removeDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float _fallDuration = 0.2f;

        private readonly List<BoardCellView> _cells = new List<BoardCellView>();
        private readonly Dictionary<Collider2D, BoardCellView> _cellLookupByCollider =
            new Dictionary<Collider2D, BoardCellView>();

        private BoardCellView[,] _cellMap;
        private BoardCellPool _cellPool;
        private bool _isResolving;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public IReadOnlyList<BoardCellView> Cells => _cells;
        public bool IsResolving => _isResolving;

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
            _swapDuration = Mathf.Max(0.01f, _swapDuration);
            _removeDuration = Mathf.Max(0.01f, _removeDuration);
            _fallDuration = Mathf.Max(0.01f, _fallDuration);
        }

        [ContextMenu("Rebuild Board")]
        public void RebuildBoard()
        {
            if (_defaultBlockPrefabs.Count == 0)
            {
                Debug.LogError("BoardGrid requires at least one default block prefab before rebuilding the board.", this);
                return;
            }

            Transform root = ResolveCellRoot();
            InitializeCellPool(root);

            ClearGeneratedCells(root);
            _cellPool.Clear();
            _cellPool.RegisterPrefabs(_defaultBlockPrefabs);

            if (_cellPool.RegisteredTypeCount == 0)
            {
                Debug.LogError("BoardGrid has no valid default block prefabs registered in the pool.", this);
                return;
            }

            _cells.Clear();
            _cellLookupByCollider.Clear();
            _cellMap = new BoardCellView[_width, _height];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    BoardCellView cell = CreateCell(root, x, y);

                    if (cell == null)
                    {
                        _cells.Add(null);
                        continue;
                    }

                    _cells.Add(cell);
                    _cellMap[x, y] = cell;
                }
            }
        }

        public BoardCellView GetCell(int x, int y)
        {
            if (!IsInsideBoard(x, y) || _cellMap == null)
            {
                return null;
            }

            return _cellMap[x, y];
        }

        public BoardCellView GetCell(Collider2D hitCollider)
        {
            if (hitCollider == null || !_cellLookupByCollider.ContainsKey(hitCollider))
            {
                return null;
            }

            return _cellLookupByCollider[hitCollider];
        }

        public Vector3 GetCellLocalPosition(int x, int y)
        {
            return GetCellPosition(x, y);
        }

        public void PreviewCellDrag(BoardCellView cell, Vector2Int direction, float distance)
        {
            if (cell == null || direction == Vector2Int.zero)
            {
                return;
            }

            Vector3 offset = new Vector3(direction.x, direction.y, 0f) * Mathf.Clamp(distance, 0f, _cellSize);
            BoardCellView targetCell = GetCell(cell.X + direction.x, cell.Y + direction.y);

            cell.transform.localPosition = GetCellPosition(cell.X, cell.Y) + offset;

            if (targetCell != null)
            {
                targetCell.transform.localPosition = GetCellPosition(targetCell.X, targetCell.Y) - offset;
            }
        }

        public void ResetCellDragPreview(BoardCellView cell, BoardCellView targetCell)
        {
            if (cell != null)
            {
                cell.transform.localPosition = GetCellPosition(cell.X, cell.Y);
            }

            if (targetCell != null)
            {
                targetCell.transform.localPosition = GetCellPosition(targetCell.X, targetCell.Y);
            }
        }

        public bool TryStartSwapCells(BoardCellView firstCell, BoardCellView secondCell)
        {
            if (_isResolving || !CanSwap(firstCell, secondCell))
            {
                return false;
            }

            StartCoroutine(SwapAndResolveRoutine(firstCell, secondCell));
            return true;
        }

        public List<BoardCellView> FindAllMatches()
        {
            HashSet<BoardCellView> matches = new HashSet<BoardCellView>();

            for (int y = 0; y < _height; y++)
            {
                AddLineMatches(matches, GetHorizontalLine(y));
            }

            for (int x = 0; x < _width; x++)
            {
                AddLineMatches(matches, GetVerticalLine(x));
            }

            return new List<BoardCellView>(matches);
        }

        private Transform ResolveCellRoot()
        {
            if (_cellRoot != null)
            {
                return _cellRoot;
            }

            return transform;
        }

        private Transform ResolvePoolRoot(Transform root)
        {
            if (_poolRoot != null)
            {
                return _poolRoot;
            }

            Transform existingPoolRoot = root.Find("CellPool");

            if (existingPoolRoot != null)
            {
                _poolRoot = existingPoolRoot;
                return _poolRoot;
            }

            GameObject poolObject = new GameObject("CellPool");
            poolObject.transform.SetParent(root, false);
            poolObject.SetActive(false);
            _poolRoot = poolObject.transform;

            return _poolRoot;
        }

        private void InitializeCellPool(Transform root)
        {
            _cellPool = new BoardCellPool(root, ResolvePoolRoot(root), ResolveInputController());
        }

        private BoardInputController ResolveInputController()
        {
            return _inputController;
        }

        private void ClearGeneratedCells(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);

                if (child == _poolRoot)
                {
                    ClearPoolRoot();
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

        private void ClearPoolRoot()
        {
            if (_poolRoot == null)
            {
                return;
            }

            for (int i = _poolRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _poolRoot.GetChild(i);

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
            BoardCellView cell = SpawnRandomCell(x, y, GetCellPosition(x, y));

            if (cell == null)
            {
                return null;
            }

            GameObject cellObject = cell.gameObject;

            cellObject.transform.SetParent(root, false);
            cellObject.transform.localPosition = GetCellPosition(x, y);
            cellObject.transform.localRotation = Quaternion.identity;
            cellObject.transform.localScale = Vector3.one * (_cellSize * 0.95f);

            cell.Configure(x, y, ResolveInputController());
            RegisterCellCollider(cell);

            return cell;
        }

        private BoardCellView SpawnRandomCell(int x, int y, Vector3 localPosition)
        {
            BlockTypeId randomType = _cellPool.GetRandomRegisteredType();
            BoardCellView cell = _cellPool.Spawn(randomType, x, y, localPosition);

            if (cell == null)
            {
                return null;
            }

            cell.transform.localScale = Vector3.one * (_cellSize * 0.95f);

            return cell;
        }

        private bool CanSwap(BoardCellView firstCell, BoardCellView secondCell)
        {
            return firstCell != null
                && secondCell != null
                && firstCell.Block != null
                && secondCell.Block != null
                && firstCell.Block.CanSwap
                && secondCell.Block.CanSwap
                && AreAdjacent(firstCell, secondCell);
        }

        private IEnumerator SwapAndResolveRoutine(BoardCellView firstCell, BoardCellView secondCell)
        {
            _isResolving = true;

            Vector3 firstTargetPosition = GetCellPosition(secondCell.X, secondCell.Y);
            Vector3 secondTargetPosition = GetCellPosition(firstCell.X, firstCell.Y);

            SetCellState(firstCell, BlockState.Swapping);
            SetCellState(secondCell, BlockState.Swapping);

            SwapCells(firstCell, secondCell, false);
            yield return AnimateCellsMove(
                firstCell,
                firstTargetPosition,
                secondCell,
                secondTargetPosition,
                _swapDuration);

            List<BoardCellView> matchedCells = FindMatchesForCells(firstCell, secondCell);

            if (matchedCells.Count == 0)
            {
                Vector3 firstRevertTarget = GetCellPosition(secondCell.X, secondCell.Y);
                Vector3 secondRevertTarget = GetCellPosition(firstCell.X, firstCell.Y);

                SwapCells(firstCell, secondCell, false);
                yield return AnimateCellsMove(
                    firstCell,
                    firstRevertTarget,
                    secondCell,
                    secondRevertTarget,
                    _swapDuration);

                SetCellState(firstCell, BlockState.Idle);
                SetCellState(secondCell, BlockState.Idle);
                _isResolving = false;
                yield break;
            }

            yield return ResolveMatchesRoutine(matchedCells);
            _isResolving = false;
        }

        private void SwapCells(BoardCellView firstCell, BoardCellView secondCell, bool moveImmediately)
        {
            Vector2Int firstPosition = new Vector2Int(firstCell.X, firstCell.Y);
            Vector2Int secondPosition = new Vector2Int(secondCell.X, secondCell.Y);

            _cellMap[firstPosition.x, firstPosition.y] = secondCell;
            _cellMap[secondPosition.x, secondPosition.y] = firstCell;

            _cells[GetCellIndex(firstPosition.x, firstPosition.y)] = secondCell;
            _cells[GetCellIndex(secondPosition.x, secondPosition.y)] = firstCell;

            firstCell.Configure(secondPosition.x, secondPosition.y, ResolveInputController());
            secondCell.Configure(firstPosition.x, firstPosition.y, ResolveInputController());

            if (moveImmediately)
            {
                firstCell.transform.localPosition = GetCellPosition(secondPosition.x, secondPosition.y);
                secondCell.transform.localPosition = GetCellPosition(firstPosition.x, firstPosition.y);
            }
        }

        private List<BoardCellView> FindMatchesForCells(BoardCellView firstCell, BoardCellView secondCell)
        {
            HashSet<BoardCellView> matches = new HashSet<BoardCellView>();

            AddLineMatches(matches, GetHorizontalLine(firstCell.Y));
            AddLineMatches(matches, GetVerticalLine(firstCell.X));
            AddLineMatches(matches, GetHorizontalLine(secondCell.Y));
            AddLineMatches(matches, GetVerticalLine(secondCell.X));

            return new List<BoardCellView>(matches);
        }

        private List<BoardCellView> GetHorizontalLine(int y)
        {
            List<BoardCellView> line = new List<BoardCellView>(_width);

            for (int x = 0; x < _width; x++)
            {
                line.Add(GetCell(x, y));
            }

            return line;
        }

        private List<BoardCellView> GetVerticalLine(int x)
        {
            List<BoardCellView> line = new List<BoardCellView>(_height);

            for (int y = 0; y < _height; y++)
            {
                line.Add(GetCell(x, y));
            }

            return line;
        }

        private static void AddLineMatches(HashSet<BoardCellView> matches, List<BoardCellView> line)
        {
            int startIndex = 0;

            while (startIndex < line.Count)
            {
                BoardCellView startCell = line[startIndex];

                if (!CanMatchCell(startCell))
                {
                    startIndex++;
                    continue;
                }

                BlockTypeId typeId = startCell.Block.TypeId;
                int endIndex = startIndex + 1;

                while (endIndex < line.Count
                    && CanMatchCell(line[endIndex])
                    && line[endIndex].Block.TypeId == typeId)
                {
                    endIndex++;
                }

                int matchLength = endIndex - startIndex;

                if (matchLength >= 3)
                {
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        matches.Add(line[i]);
                    }
                }

                startIndex = endIndex;
            }
        }

        private static bool CanMatchCell(BoardCellView cell)
        {
            return cell != null && cell.Block != null && cell.Block.CanMatch;
        }

        private static bool AreAdjacent(BoardCellView firstCell, BoardCellView secondCell)
        {
            int distanceX = Mathf.Abs(firstCell.X - secondCell.X);
            int distanceY = Mathf.Abs(firstCell.Y - secondCell.Y);

            return distanceX + distanceY == 1;
        }

        private static void MarkMatchedCells(List<BoardCellView> matchedCells)
        {
            for (int i = 0; i < matchedCells.Count; i++)
            {
                matchedCells[i].Block.SetState(BlockState.Matched);
            }
        }

        private IEnumerator AnimateCellsMove(
            BoardCellView firstCell,
            Vector3 firstTargetPosition,
            BoardCellView secondCell,
            Vector3 secondTargetPosition,
            float duration)
        {
#if DOTWEEN
            Sequence sequence = DOTween.Sequence();
            sequence.Join(firstCell.transform.DOLocalMove(firstTargetPosition, duration).SetEase(Ease.InOutSine));
            sequence.Join(secondCell.transform.DOLocalMove(secondTargetPosition, duration).SetEase(Ease.InOutSine));
            yield return sequence.WaitForCompletion();
#else
            Vector3 firstStartPosition = firstCell.transform.localPosition;
            Vector3 secondStartPosition = secondCell.transform.localPosition;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

                firstCell.transform.localPosition = Vector3.Lerp(firstStartPosition, firstTargetPosition, easedProgress);
                secondCell.transform.localPosition = Vector3.Lerp(secondStartPosition, secondTargetPosition, easedProgress);

                yield return null;
            }

            firstCell.transform.localPosition = firstTargetPosition;
            secondCell.transform.localPosition = secondTargetPosition;
#endif
        }

        private IEnumerator ResolveMatchesRoutine(List<BoardCellView> matchedCells)
        {
            while (matchedCells.Count > 0)
            {
                MarkMatchedCells(matchedCells);
                yield return AnimateMatchedCellsRemove(matchedCells);
                RemoveMatchedCells(matchedCells);

                yield return CollapseAndRefillRoutine();

                matchedCells = FindAllMatches();
            }
        }

        private IEnumerator CollapseAndRefillRoutine()
        {
            List<CellMove> moves = new List<CellMove>();
            Transform root = ResolveCellRoot();

            for (int x = 0; x < _width; x++)
            {
                int writeY = 0;

                for (int y = 0; y < _height; y++)
                {
                    BoardCellView cell = _cellMap[x, y];

                    if (cell == null)
                    {
                        continue;
                    }

                    if (y != writeY)
                    {
                        _cellMap[x, writeY] = cell;
                        _cellMap[x, y] = null;
                        _cells[GetCellIndex(x, writeY)] = cell;
                        _cells[GetCellIndex(x, y)] = null;

                        cell.Configure(x, writeY, ResolveInputController());
                        SetCellState(cell, BlockState.Falling);
                        moves.Add(new CellMove(cell, GetCellPosition(x, writeY)));
                    }

                    writeY++;
                }

                for (int y = writeY; y < _height; y++)
                {
                    int spawnOffset = y - writeY + 1;
                    Vector3 spawnPosition = GetCellPosition(x, _height - 1 + spawnOffset);
                    BoardCellView spawnedCell = SpawnRandomCell(x, y, spawnPosition);

                    if (spawnedCell == null)
                    {
                        continue;
                    }

                    spawnedCell.transform.SetParent(root, false);
                    SetCellState(spawnedCell, BlockState.Spawning);

                    _cellMap[x, y] = spawnedCell;
                    _cells[GetCellIndex(x, y)] = spawnedCell;
                    RegisterCellCollider(spawnedCell);
                    moves.Add(new CellMove(spawnedCell, GetCellPosition(x, y)));
                }
            }

            if (moves.Count == 0)
            {
                yield break;
            }

            yield return AnimateCellMoves(moves, _fallDuration);

            for (int i = 0; i < moves.Count; i++)
            {
                SetCellState(moves[i].Cell, BlockState.Idle);
            }
        }

        private IEnumerator AnimateCellMoves(List<CellMove> moves, float duration)
        {
#if DOTWEEN
            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].Cell == null)
                {
                    continue;
                }

                sequence.Join(moves[i].Cell.transform.DOLocalMove(moves[i].TargetPosition, duration).SetEase(Ease.InOutSine));
            }

            yield return sequence.WaitForCompletion();
#else
            Vector3[] startPositions = new Vector3[moves.Count];

            for (int i = 0; i < moves.Count; i++)
            {
                startPositions[i] = moves[i].Cell.transform.localPosition;
            }

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

                for (int i = 0; i < moves.Count; i++)
                {
                    if (moves[i].Cell == null)
                    {
                        continue;
                    }

                    moves[i].Cell.transform.localPosition = Vector3.Lerp(
                        startPositions[i],
                        moves[i].TargetPosition,
                        easedProgress);
                }

                yield return null;
            }

            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].Cell != null)
                {
                    moves[i].Cell.transform.localPosition = moves[i].TargetPosition;
                }
            }
#endif
        }

        private IEnumerator AnimateMatchedCellsRemove(List<BoardCellView> matchedCells)
        {
#if DOTWEEN
            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < matchedCells.Count; i++)
            {
                if (matchedCells[i] == null)
                {
                    continue;
                }

                sequence.Join(matchedCells[i].transform.DOScale(Vector3.zero, _removeDuration).SetEase(Ease.InBack));
            }

            yield return sequence.WaitForCompletion();
#else
            Vector3[] startScales = new Vector3[matchedCells.Count];

            for (int i = 0; i < matchedCells.Count; i++)
            {
                startScales[i] = matchedCells[i].transform.localScale;
            }

            float elapsedTime = 0f;

            while (elapsedTime < _removeDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / _removeDuration);
                float scaleProgress = 1f - Mathf.SmoothStep(0f, 1f, progress);

                for (int i = 0; i < matchedCells.Count; i++)
                {
                    if (matchedCells[i] == null)
                    {
                        continue;
                    }

                    matchedCells[i].transform.localScale = startScales[i] * scaleProgress;
                }

                yield return null;
            }
#endif
        }

        private void RemoveMatchedCells(List<BoardCellView> matchedCells)
        {
            for (int i = 0; i < matchedCells.Count; i++)
            {
                BoardCellView cell = matchedCells[i];

                if (cell == null)
                {
                    continue;
                }

                int x = cell.X;
                int y = cell.Y;

                if (IsInsideBoard(x, y) && _cellMap[x, y] == cell)
                {
                    _cellMap[x, y] = null;
                }

                int cellIndex = GetCellIndex(x, y);

                if (cellIndex >= 0 && cellIndex < _cells.Count && _cells[cellIndex] == cell)
                {
                    _cells[cellIndex] = null;
                }

                UnregisterCellCollider(cell);
                _cellPool.Despawn(cell);
            }
        }

        private void RegisterCellCollider(BoardCellView cell)
        {
            if (cell == null || cell.HitCollider == null)
            {
                return;
            }

            if (_cellLookupByCollider.ContainsKey(cell.HitCollider))
            {
                _cellLookupByCollider[cell.HitCollider] = cell;
                return;
            }

            _cellLookupByCollider.Add(cell.HitCollider, cell);
        }

        private void UnregisterCellCollider(BoardCellView cell)
        {
            if (cell == null || cell.HitCollider == null)
            {
                return;
            }

            if (_cellLookupByCollider.ContainsKey(cell.HitCollider))
            {
                _cellLookupByCollider.Remove(cell.HitCollider);
            }
        }

        private static void SetCellState(BoardCellView cell, BlockState state)
        {
            if (cell != null && cell.Block != null)
            {
                cell.Block.SetState(state);
            }
        }

        private int GetCellIndex(int x, int y)
        {
            return y * _width + x;
        }

        private bool IsInsideBoard(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        private Vector3 GetCellPosition(int x, int y)
        {
            float positionX = x * _cellSize;
            float positionY = y * _cellSize;

            return new Vector3(positionX, positionY, 0f);
        }

        private readonly struct CellMove
        {
            public readonly BoardCellView Cell;
            public readonly Vector3 TargetPosition;

            public CellMove(BoardCellView cell, Vector3 targetPosition)
            {
                Cell = cell;
                TargetPosition = targetPosition;
            }
        }
    }
}
