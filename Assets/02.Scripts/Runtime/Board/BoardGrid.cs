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
        [SerializeField] private List<BoardCellView> _itemBlockPrefabs = new List<BoardCellView>();
        [SerializeField] private BoardInputController _inputController;

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

        public void Initialize(BoardInputController inputController)
        {
            _inputController = inputController;
            NormalizeSettings();
        }

        private void NormalizeSettings()
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
            NormalizeSettings();

            if (_defaultBlockPrefabs.Count == 0)
            {
                Debug.LogError("BoardGrid requires at least one default block prefab before rebuilding the board.", this);
                return;
            }

            Transform root = ResolveCellRoot();
            InitializeCellPool(root);

            ClearGeneratedCells(root);
            _cellPool.Clear();
            _cellPool.RegisterPrefabs(GetAllBlockPrefabs());

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

            if (Application.isPlaying)
            {
                StartCoroutine(ResolveInitialMatchesRoutine());
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

        public bool TryActivateItemCell(BoardCellView cell)
        {
            if (_isResolving || cell == null || cell.Block == null || !cell.Block.CanActivate)
            {
                return false;
            }

            StartCoroutine(ActivateItemCellRoutine(cell));
            return true;
        }

        public List<BoardCellView> FindAllMatches()
        {
            HashSet<BoardCellView> matches = new HashSet<BoardCellView>();
            List<MatchGroup> matchGroups = FindMatchGroups();

            for (int i = 0; i < matchGroups.Count; i++)
            {
                matchGroups[i].AddCellsTo(matches);
            }

            return new List<BoardCellView>(matches);
        }

        private List<BoardCellView> GetAllBlockPrefabs()
        {
            List<BoardCellView> prefabs = new List<BoardCellView>(_defaultBlockPrefabs.Count + _itemBlockPrefabs.Count);
            prefabs.AddRange(_defaultBlockPrefabs);
            prefabs.AddRange(_itemBlockPrefabs);

            return prefabs;
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

            List<BoardCellView> matchedCells = FindAllMatches();
            List<BoardCellView> activatedItemCells = FindActivatedItemCells(firstCell, secondCell);
            SpecialBlockSpawn specialBlockSpawn = FindSpecialBlockSpawn(firstCell, secondCell);

            if (matchedCells.Count == 0 && activatedItemCells.Count == 0)
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

            if (activatedItemCells.Count > 0)
            {
                specialBlockSpawn = SpecialBlockSpawn.None;
            }

            AddUniqueCells(matchedCells, activatedItemCells);
            yield return ResolveMatchesRoutine(matchedCells, specialBlockSpawn);
            _isResolving = false;
        }

        private IEnumerator ActivateItemCellRoutine(BoardCellView cell)
        {
            _isResolving = true;

            List<BoardCellView> activatedItemCells = FindActivatedItemCells(cell, cell);
            yield return ResolveMatchesRoutine(activatedItemCells);

            _isResolving = false;
        }

        private List<BoardCellView> FindActivatedItemCells(BoardCellView firstCell, BoardCellView secondCell)
        {
            HashSet<BoardCellView> affectedCells = new HashSet<BoardCellView>();
            Queue<ItemActivation> activations = new Queue<ItemActivation>();
            HashSet<BlockBase> queuedItems = new HashSet<BlockBase>();

            EnqueueActivation(firstCell.Block, secondCell.Block, activations, queuedItems);
            EnqueueActivation(secondCell.Block, firstCell.Block, activations, queuedItems);

            while (activations.Count > 0)
            {
                ItemActivation activation = activations.Dequeue();

                if (activation.ItemBlock.TypeId == BlockTypeId.Bomb)
                {
                    AddBombAffectedCells(activation, affectedCells, activations, queuedItems);
                    continue;
                }

                if (activation.ItemBlock.TypeId == BlockTypeId.ColorBomb)
                {
                    AddColorBombAffectedCells(activation, affectedCells, activations, queuedItems);
                    continue;
                }

                for (int i = 0; i < _cells.Count; i++)
                {
                    BoardCellView candidateCell = _cells[i];

                    if (candidateCell == null || candidateCell.Block == null)
                    {
                        continue;
                    }

                    if (!activation.ItemBlock.AffectsCell(
                        activation.TargetBlock,
                        candidateCell.Block,
                        _width,
                        _height))
                    {
                        continue;
                    }

                    affectedCells.Add(candidateCell);
                    EnqueueActivation(candidateCell.Block, activation.TargetBlock, activations, queuedItems);
                }
            }

            return new List<BoardCellView>(affectedCells);
        }

        private void AddBombAffectedCells(
            ItemActivation activation,
            HashSet<BoardCellView> affectedCells,
            Queue<ItemActivation> activations,
            HashSet<BlockBase> queuedItems)
        {
            BoardCellView bombCell = GetCell(activation.ItemBlock.BoardPosition.x, activation.ItemBlock.BoardPosition.y);
            AddAffectedCell(bombCell, activation, affectedCells, activations, queuedItems);

            BoardCellView targetCell = GetBombTargetCell(activation);
            AddAffectedCell(targetCell, activation, affectedCells, activations, queuedItems);
        }

        private BoardCellView GetBombTargetCell(ItemActivation activation)
        {
            if (activation.TargetBlock != activation.ItemBlock && activation.TargetBlock.CanMatch)
            {
                return GetCell(activation.TargetBlock.BoardPosition.x, activation.TargetBlock.BoardPosition.y);
            }

            BoardCellView bestCell = null;
            int bestDistance = int.MaxValue;
            Vector2Int itemPosition = activation.ItemBlock.BoardPosition;

            for (int i = 0; i < _cells.Count; i++)
            {
                BoardCellView candidateCell = _cells[i];

                if (!CanMatchCell(candidateCell))
                {
                    continue;
                }

                int distance = Mathf.Abs(candidateCell.X - itemPosition.x) + Mathf.Abs(candidateCell.Y - itemPosition.y);

                if (distance < bestDistance)
                {
                    bestCell = candidateCell;
                    bestDistance = distance;
                }
            }

            return bestCell;
        }

        private void AddColorBombAffectedCells(
            ItemActivation activation,
            HashSet<BoardCellView> affectedCells,
            Queue<ItemActivation> activations,
            HashSet<BlockBase> queuedItems)
        {
            BoardCellView colorBombCell = GetCell(activation.ItemBlock.BoardPosition.x, activation.ItemBlock.BoardPosition.y);
            AddAffectedCell(colorBombCell, activation, affectedCells, activations, queuedItems);

            BlockTypeId targetType = GetColorBombTargetType(activation);

            if (targetType == BlockTypeId.None)
            {
                return;
            }

            for (int i = 0; i < _cells.Count; i++)
            {
                BoardCellView candidateCell = _cells[i];

                if (!CanMatchCell(candidateCell) || candidateCell.Block.TypeId != targetType)
                {
                    continue;
                }

                AddAffectedCell(candidateCell, activation, affectedCells, activations, queuedItems);
            }
        }

        private BlockTypeId GetColorBombTargetType(ItemActivation activation)
        {
            if (activation.TargetBlock != activation.ItemBlock && activation.TargetBlock.CanMatch)
            {
                return activation.TargetBlock.TypeId;
            }

            return GetRandomBoardColorType();
        }

        private BlockTypeId GetRandomBoardColorType()
        {
            List<BlockTypeId> colorTypes = new List<BlockTypeId>();

            for (int i = 0; i < _cells.Count; i++)
            {
                BoardCellView candidateCell = _cells[i];

                if (!CanMatchCell(candidateCell) || colorTypes.Contains(candidateCell.Block.TypeId))
                {
                    continue;
                }

                colorTypes.Add(candidateCell.Block.TypeId);
            }

            if (colorTypes.Count == 0)
            {
                return BlockTypeId.None;
            }

            return colorTypes[Random.Range(0, colorTypes.Count)];
        }

        private static void AddAffectedCell(
            BoardCellView candidateCell,
            ItemActivation activation,
            HashSet<BoardCellView> affectedCells,
            Queue<ItemActivation> activations,
            HashSet<BlockBase> queuedItems)
        {
            if (candidateCell == null || candidateCell.Block == null)
            {
                return;
            }

            affectedCells.Add(candidateCell);
            EnqueueActivation(candidateCell.Block, activation.TargetBlock, activations, queuedItems);
        }

        private static void EnqueueActivation(
            BlockBase itemBlock,
            BlockBase targetBlock,
            Queue<ItemActivation> activations,
            HashSet<BlockBase> queuedItems)
        {
            if (!itemBlock.CanActivate || queuedItems.Contains(itemBlock))
            {
                return;
            }

            queuedItems.Add(itemBlock);
            activations.Enqueue(new ItemActivation(itemBlock, targetBlock));
        }

        private static void AddUniqueCells(List<BoardCellView> targetCells, List<BoardCellView> cellsToAdd)
        {
            HashSet<BoardCellView> existingCells = new HashSet<BoardCellView>(targetCells);

            for (int i = 0; i < cellsToAdd.Count; i++)
            {
                if (existingCells.Add(cellsToAdd[i]))
                {
                    targetCells.Add(cellsToAdd[i]);
                }
            }
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

        private List<MatchGroup> FindMatchGroups()
        {
            List<MatchGroup> matchGroups = new List<MatchGroup>();

            for (int y = 0; y < _height; y++)
            {
                AddLineMatchGroups(matchGroups, GetHorizontalLine(y), MatchDirection.Horizontal);
            }

            for (int x = 0; x < _width; x++)
            {
                AddLineMatchGroups(matchGroups, GetVerticalLine(x), MatchDirection.Vertical);
            }

            AddSquareMatchGroups(matchGroups);

            return matchGroups;
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

        private static void AddLineMatchGroups(
            List<MatchGroup> matchGroups,
            List<BoardCellView> line,
            MatchDirection direction)
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
                    List<BoardCellView> cells = new List<BoardCellView>(matchLength);

                    for (int i = startIndex; i < endIndex; i++)
                    {
                        cells.Add(line[i]);
                    }

                    matchGroups.Add(new MatchGroup(cells, direction));
                }

                startIndex = endIndex;
            }
        }

        private void AddSquareMatchGroups(List<MatchGroup> matchGroups)
        {
            for (int y = 0; y < _height - 1; y++)
            {
                for (int x = 0; x < _width - 1; x++)
                {
                    BoardCellView bottomLeft = GetCell(x, y);
                    BoardCellView bottomRight = GetCell(x + 1, y);
                    BoardCellView topLeft = GetCell(x, y + 1);
                    BoardCellView topRight = GetCell(x + 1, y + 1);

                    if (!CanMatchSquare(bottomLeft, bottomRight, topLeft, topRight))
                    {
                        continue;
                    }

                    matchGroups.Add(new MatchGroup(
                        new List<BoardCellView>
                        {
                            bottomLeft,
                            bottomRight,
                            topLeft,
                            topRight
                        },
                        MatchDirection.Square));
                }
            }
        }

        private SpecialBlockSpawn FindSpecialBlockSpawn(BoardCellView firstCell, BoardCellView secondCell)
        {
            List<MatchGroup> matchGroups = FindMatchGroups();
            SpecialBlockSpawn bestSpawn = SpecialBlockSpawn.None;

            for (int i = 0; i < matchGroups.Count; i++)
            {
                MatchGroup matchGroup = matchGroups[i];
                BlockTypeId specialType = GetSpecialBlockType(matchGroup);

                if (specialType == BlockTypeId.None)
                {
                    continue;
                }

                BoardCellView spawnCell = GetSpecialSpawnCell(matchGroup, firstCell, secondCell);

                if (spawnCell == null)
                {
                    continue;
                }

                int priority = GetSpecialBlockPriority(specialType);

                if (!bestSpawn.HasValue || priority > bestSpawn.Priority)
                {
                    bestSpawn = new SpecialBlockSpawn(specialType, spawnCell.X, spawnCell.Y, priority);
                }
            }

            return bestSpawn;
        }

        private static BoardCellView GetSpecialSpawnCell(
            MatchGroup matchGroup,
            BoardCellView firstCell,
            BoardCellView secondCell)
        {
            if (matchGroup.Contains(firstCell))
            {
                return firstCell;
            }

            if (matchGroup.Contains(secondCell))
            {
                return secondCell;
            }

            return null;
        }

        private static BlockTypeId GetSpecialBlockType(MatchGroup matchGroup)
        {
            if (matchGroup.Direction == MatchDirection.Square)
            {
                return BlockTypeId.Bomb;
            }

            if (matchGroup.Count >= 5)
            {
                return BlockTypeId.ColorBomb;
            }

            if (matchGroup.Count == 4 && matchGroup.Direction == MatchDirection.Horizontal)
            {
                return BlockTypeId.HorizontalRocket;
            }

            if (matchGroup.Count == 4 && matchGroup.Direction == MatchDirection.Vertical)
            {
                return BlockTypeId.VerticalRocket;
            }

            return BlockTypeId.None;
        }

        private static int GetSpecialBlockPriority(BlockTypeId typeId)
        {
            if (typeId == BlockTypeId.ColorBomb)
            {
                return 3;
            }

            if (typeId == BlockTypeId.Bomb)
            {
                return 2;
            }

            return 1;
        }

        private static bool CanMatchSquare(
            BoardCellView bottomLeft,
            BoardCellView bottomRight,
            BoardCellView topLeft,
            BoardCellView topRight)
        {
            if (!CanMatchCell(bottomLeft)
                || !CanMatchCell(bottomRight)
                || !CanMatchCell(topLeft)
                || !CanMatchCell(topRight))
            {
                return false;
            }

            BlockTypeId typeId = bottomLeft.Block.TypeId;

            return bottomRight.Block.TypeId == typeId
                && topLeft.Block.TypeId == typeId
                && topRight.Block.TypeId == typeId;
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
            yield return ResolveMatchesRoutine(matchedCells, SpecialBlockSpawn.None);
        }

        private IEnumerator ResolveMatchesRoutine(List<BoardCellView> matchedCells, SpecialBlockSpawn specialBlockSpawn)
        {
            while (matchedCells.Count > 0)
            {
                if (specialBlockSpawn.HasValue)
                {
                    RemoveSpecialSpawnCellFromMatches(matchedCells, specialBlockSpawn);
                }

                MarkMatchedCells(matchedCells);
                yield return AnimateMatchedCellsRemove(matchedCells);
                RemoveMatchedCells(matchedCells);

                if (specialBlockSpawn.HasValue)
                {
                    SpawnSpecialBlock(specialBlockSpawn);
                    specialBlockSpawn = SpecialBlockSpawn.None;
                }

                yield return CollapseAndRefillRoutine();

                matchedCells = FindAllMatches();
            }
        }

        private static void RemoveSpecialSpawnCellFromMatches(
            List<BoardCellView> matchedCells,
            SpecialBlockSpawn specialBlockSpawn)
        {
            for (int i = matchedCells.Count - 1; i >= 0; i--)
            {
                BoardCellView cell = matchedCells[i];

                if (cell.X == specialBlockSpawn.X && cell.Y == specialBlockSpawn.Y)
                {
                    matchedCells.RemoveAt(i);
                }
            }
        }

        private void SpawnSpecialBlock(SpecialBlockSpawn specialBlockSpawn)
        {
            BoardCellView existingCell = GetCell(specialBlockSpawn.X, specialBlockSpawn.Y);

            if (existingCell != null)
            {
                UnregisterCellCollider(existingCell);
                _cellPool.Despawn(existingCell);
            }

            BoardCellView specialCell = _cellPool.Spawn(
                specialBlockSpawn.TypeId,
                specialBlockSpawn.X,
                specialBlockSpawn.Y,
                GetCellPosition(specialBlockSpawn.X, specialBlockSpawn.Y));

            specialCell.transform.localScale = Vector3.one * (_cellSize * 0.95f);
            _cellMap[specialBlockSpawn.X, specialBlockSpawn.Y] = specialCell;
            _cells[GetCellIndex(specialBlockSpawn.X, specialBlockSpawn.Y)] = specialCell;
            RegisterCellCollider(specialCell);
        }

        private IEnumerator ResolveInitialMatchesRoutine()
        {
            List<BoardCellView> matchedCells = FindAllMatches();

            if (matchedCells.Count == 0)
            {
                yield break;
            }

            _isResolving = true;
            yield return ResolveMatchesRoutine(matchedCells);
            _isResolving = false;
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

        private readonly struct ItemActivation
        {
            public readonly BlockBase ItemBlock;
            public readonly BlockBase TargetBlock;

            public ItemActivation(BlockBase itemBlock, BlockBase targetBlock)
            {
                ItemBlock = itemBlock;
                TargetBlock = targetBlock;
            }
        }

        private readonly struct MatchGroup
        {
            private readonly List<BoardCellView> _cells;

            public readonly MatchDirection Direction;
            public int Count => _cells.Count;

            public MatchGroup(List<BoardCellView> cells, MatchDirection direction)
            {
                _cells = cells;
                Direction = direction;
            }

            public bool Contains(BoardCellView cell)
            {
                return _cells.Contains(cell);
            }

            public void AddCellsTo(HashSet<BoardCellView> targetCells)
            {
                for (int i = 0; i < _cells.Count; i++)
                {
                    targetCells.Add(_cells[i]);
                }
            }
        }

        private readonly struct SpecialBlockSpawn
        {
            public static readonly SpecialBlockSpawn None = new SpecialBlockSpawn(BlockTypeId.None, -1, -1, 0);

            public readonly BlockTypeId TypeId;
            public readonly int X;
            public readonly int Y;
            public readonly int Priority;

            public bool HasValue => TypeId != BlockTypeId.None;

            public SpecialBlockSpawn(BlockTypeId typeId, int x, int y, int priority)
            {
                TypeId = typeId;
                X = x;
                Y = y;
                Priority = priority;
            }
        }

        private enum MatchDirection
        {
            Horizontal,
            Vertical,
            Square
        }
    }
}
