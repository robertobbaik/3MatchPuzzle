using System.Collections.Generic;
using UnityEngine;
using ThreeMatch.Tiles;

namespace ThreeMatch.Board
{
    public sealed class BoardCellPool
    {
        private readonly Dictionary<BlockTypeId, BoardCellView> _prefabs = new Dictionary<BlockTypeId, BoardCellView>();
        private readonly Dictionary<BlockTypeId, Queue<BoardCellView>> _pooledCells = new Dictionary<BlockTypeId, Queue<BoardCellView>>();
        private readonly List<BlockTypeId> _registeredTypes = new List<BlockTypeId>();

        private readonly Transform _activeRoot;
        private readonly Transform _poolRoot;
        private readonly BoardInputController _inputController;

        public int RegisteredTypeCount => _registeredTypes.Count;

        public BoardCellPool(Transform activeRoot, Transform poolRoot, BoardInputController inputController)
        {
            _activeRoot = activeRoot;
            _poolRoot = poolRoot;
            _inputController = inputController;
        }

        public void RegisterPrefabs(IReadOnlyList<BoardCellView> prefabs)
        {
            _prefabs.Clear();
            _pooledCells.Clear();
            _registeredTypes.Clear();

            for (int i = 0; i < prefabs.Count; i++)
            {
                BoardCellView prefab = prefabs[i];

                if (prefab == null || prefab.Block == null)
                {
                    continue;
                }

                BlockTypeId typeId = prefab.Block.TypeId;

                if (_prefabs.ContainsKey(typeId))
                {
                    continue;
                }

                _prefabs.Add(typeId, prefab);
                _pooledCells.Add(typeId, new Queue<BoardCellView>());

                if (prefab.Block.CanMatch)
                {
                    _registeredTypes.Add(typeId);
                }
            }
        }

        public BlockTypeId GetRandomRegisteredType()
        {
            int randomIndex = Random.Range(0, _registeredTypes.Count);
            return _registeredTypes[randomIndex];
        }

        public BoardCellView Spawn(BlockTypeId typeId, int x, int y, Vector3 localPosition)
        {
            BoardCellView cell = GetOrCreateCell(typeId);

            if (cell == null)
            {
                return null;
            }

            Transform cellTransform = cell.transform;

            cellTransform.SetParent(_activeRoot, false);
            cellTransform.localPosition = localPosition;
            cellTransform.localRotation = Quaternion.identity;
            cellTransform.localScale = Vector3.one;

            cell.gameObject.SetActive(true);
            cell.Configure(x, y, _inputController);
            cell.Block.SetState(BlockState.Idle);

            return cell;
        }

        public void Despawn(BoardCellView cell)
        {
            if (cell == null || cell.Block == null)
            {
                return;
            }

            BlockTypeId typeId = cell.Block.TypeId;

            if (!_pooledCells.ContainsKey(typeId))
            {
                Object.Destroy(cell.gameObject);
                return;
            }

            Queue<BoardCellView> pool = _pooledCells[typeId];

            cell.Block.SetState(BlockState.Idle);
            cell.gameObject.SetActive(false);
            cell.transform.SetParent(_poolRoot, false);
            cell.transform.localScale = Vector3.one;
            pool.Enqueue(cell);
        }

        public void Clear()
        {
            foreach (Queue<BoardCellView> pool in _pooledCells.Values)
            {
                while (pool.Count > 0)
                {
                    BoardCellView cell = pool.Dequeue();

                    if (cell != null)
                    {
                        Object.Destroy(cell.gameObject);
                    }
                }
            }
        }

        private BoardCellView GetOrCreateCell(BlockTypeId typeId)
        {
            if (!_prefabs.ContainsKey(typeId))
            {
                Debug.LogError($"BoardCellPool has no prefab registered for {typeId}.");
                return null;
            }

            BoardCellView prefab = _prefabs[typeId];
            Queue<BoardCellView> pool = _pooledCells[typeId];

            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }

            return Object.Instantiate(prefab, _activeRoot);
        }
    }
}
