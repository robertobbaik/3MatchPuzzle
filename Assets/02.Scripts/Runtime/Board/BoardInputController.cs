using UnityEngine;
using UnityEngine.InputSystem;
using ThreeMatch.Tiles;

namespace ThreeMatch.Board
{
    public sealed class BoardInputController : MonoBehaviour
    {
        [SerializeField] private BoardGrid _boardGrid;
        [SerializeField] private Camera _inputCamera;
        [SerializeField] private LayerMask _cellLayerMask = Physics2D.DefaultRaycastLayers;
        [SerializeField, Min(1f)] private float _dragThresholdPixels = 35f;
        [SerializeField, Range(0.1f, 1f)] private float _dragPreviewMaxCellRatio = 0.95f;

        private BoardCellView _selectedCell;
        private BoardCellView _pressedCell;
        private BoardCellView _dragTargetCell;
        private Vector2 _pressScreenPosition;
        private Vector3 _pressLocalPosition;
        private Vector2Int _dragDirection;
        private bool _isDragging;

        public BoardGrid BoardGrid => _boardGrid;
        public BoardCellView SelectedCell => _selectedCell;

        private void Awake()
        {
            ResolveBoardGrid();
            ResolveInputCamera();
        }

        private void Update()
        {
            if (_boardGrid != null && _boardGrid.IsResolving)
            {
                return;
            }

            PointerFrame pointerFrame = ReadPointerFrame();

            if (!pointerFrame.HasInput)
            {
                return;
            }

            if (pointerFrame.PressedThisFrame)
            {
                HandleCellPressBegan(FindCellAtScreenPosition(pointerFrame.ScreenPosition), pointerFrame.ScreenPosition);
            }

            if (pointerFrame.IsPressed)
            {
                HandleCellPressMoved(pointerFrame.ScreenPosition);
            }

            if (pointerFrame.ReleasedThisFrame)
            {
                HandleCellPressEnded(_pressedCell, pointerFrame.ScreenPosition);
            }
        }

        private void OnValidate()
        {
            ResolveInputCamera();
            _dragPreviewMaxCellRatio = Mathf.Clamp(_dragPreviewMaxCellRatio, 0.1f, 1f);
        }

        public void HandleCellPressBegan(BoardCellView cell, Vector2 screenPosition)
        {
            if (!CanUseCell(cell))
            {
                return;
            }

            _pressedCell = cell;
            _pressScreenPosition = screenPosition;
            _pressLocalPosition = ScreenToBoardLocalPosition(screenPosition);
            _dragTargetCell = null;
            _dragDirection = Vector2Int.zero;
            _isDragging = false;
        }

        public void HandleCellPressEnded(BoardCellView cell, Vector2 screenPosition)
        {
            if (_pressedCell == null || _pressedCell != cell)
            {
                ClearDragState(true);
                return;
            }

            Vector2 dragDelta = screenPosition - _pressScreenPosition;

            if (dragDelta.magnitude < _dragThresholdPixels)
            {
                ClearDragPreview();
                HandleCellPressed(cell);
                ClearDragState(false);
                return;
            }

            if (_isDragging && _dragTargetCell != null)
            {
                if (!RequestSwap(cell, _dragTargetCell))
                {
                    ClearDragState(true);
                    return;
                }

                ClearSelection();
                ClearDragState(false);
                return;
            }

            ClearDragState(true);
        }

        public void HandleCellPressMoved(Vector2 screenPosition)
        {
            if (_pressedCell == null || _boardGrid == null)
            {
                return;
            }

            Vector2 dragDelta = screenPosition - _pressScreenPosition;

            if (dragDelta.magnitude < _dragThresholdPixels)
            {
                ClearDragPreview();
                return;
            }

            Vector2Int direction = GetDragDirection(dragDelta);

            if (_dragDirection != direction)
            {
                ClearDragPreview();
                _dragDirection = direction;
                _dragTargetCell = _boardGrid.GetCell(_pressedCell.X + direction.x, _pressedCell.Y + direction.y);
            }

            Vector3 currentLocalPosition = ScreenToBoardLocalPosition(screenPosition);
            Vector3 localDelta = currentLocalPosition - _pressLocalPosition;
            float axisDistance = GetAxisDistance(localDelta, direction);
            float maxPreviewDistance = _boardGrid.CellSize * _dragPreviewMaxCellRatio;
            float clampedDistance = Mathf.Clamp(axisDistance, 0f, maxPreviewDistance);

            _isDragging = clampedDistance > 0f;

            if (_dragTargetCell == null)
            {
                _pressedCell.transform.localPosition =
                    _boardGrid.GetCellLocalPosition(_pressedCell.X, _pressedCell.Y)
                    + new Vector3(direction.x, direction.y, 0f) * clampedDistance * 0.25f;
                return;
            }

            _boardGrid.PreviewCellDrag(_pressedCell, direction, clampedDistance);
        }

        public void HandleCellPressed(BoardCellView cell)
        {
            if (!CanUseCell(cell))
            {
                return;
            }

            if (_selectedCell == null)
            {
                SelectCell(cell);
                return;
            }

            if (_selectedCell == cell)
            {
                ClearSelection();
                return;
            }

            if (AreAdjacent(_selectedCell, cell))
            {
                RequestSwap(_selectedCell, cell);
                ClearSelection();
                return;
            }

            SelectCell(cell);
        }

        private void HandleCellDragged(BoardCellView cell, Vector2 dragDelta)
        {
            if (!CanUseCell(cell) || _boardGrid == null)
            {
                return;
            }

            Vector2Int direction = GetDragDirection(dragDelta);
            BoardCellView targetCell = _boardGrid.GetCell(cell.X + direction.x, cell.Y + direction.y);

            RequestSwap(cell, targetCell);
            ClearSelection();
        }

        private void ResolveBoardGrid()
        {
            if (_boardGrid != null)
            {
                return;
            }

            Debug.LogError("BoardInputController requires a serialized BoardGrid reference.", this);
        }

        private void ResolveInputCamera()
        {
            if (_inputCamera != null)
            {
                return;
            }

            _inputCamera = Camera.main;
        }

        private void SelectCell(BoardCellView cell)
        {
            ClearSelection();

            _selectedCell = cell;
            _selectedCell.Block.SetState(BlockState.Selected);
        }

        private void ClearSelection()
        {
            if (_selectedCell != null && _selectedCell.Block != null)
            {
                _selectedCell.Block.SetState(BlockState.Idle);
            }

            _selectedCell = null;
        }

        private static bool CanUseCell(BoardCellView cell)
        {
            return cell != null && cell.Block != null && cell.Block.CanSwap;
        }

        private static bool AreAdjacent(BoardCellView firstCell, BoardCellView secondCell)
        {
            int distanceX = Mathf.Abs(firstCell.X - secondCell.X);
            int distanceY = Mathf.Abs(firstCell.Y - secondCell.Y);

            return distanceX + distanceY == 1;
        }

        private bool RequestSwap(BoardCellView firstCell, BoardCellView secondCell)
        {
            if (_boardGrid == null || secondCell == null)
            {
                return false;
            }

            return _boardGrid.TryStartSwapCells(firstCell, secondCell);
        }

        private static Vector2Int GetDragDirection(Vector2 dragDelta)
        {
            if (Mathf.Abs(dragDelta.x) >= Mathf.Abs(dragDelta.y))
            {
                return dragDelta.x > 0f ? Vector2Int.right : Vector2Int.left;
            }

            return dragDelta.y > 0f ? Vector2Int.up : Vector2Int.down;
        }

        private void ClearDragState(bool resetPreview)
        {
            if (resetPreview)
            {
                ClearDragPreview();
            }

            _pressedCell = null;
            _dragTargetCell = null;
            _pressScreenPosition = Vector2.zero;
            _pressLocalPosition = Vector3.zero;
            _dragDirection = Vector2Int.zero;
            _isDragging = false;
        }

        private void ClearDragPreview()
        {
            if (_boardGrid == null || _pressedCell == null)
            {
                return;
            }

            _boardGrid.ResetCellDragPreview(_pressedCell, _dragTargetCell);
        }

        private Vector3 ScreenToBoardLocalPosition(Vector2 screenPosition)
        {
            if (_inputCamera == null)
            {
                ResolveInputCamera();
            }

            if (_inputCamera == null || _boardGrid == null)
            {
                return Vector3.zero;
            }

            float cameraDistance = Mathf.Abs(_inputCamera.transform.position.z - _boardGrid.transform.position.z);
            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, cameraDistance);
            Vector3 worldPosition = _inputCamera.ScreenToWorldPoint(screenPoint);

            return _boardGrid.transform.InverseTransformPoint(worldPosition);
        }

        private static float GetAxisDistance(Vector3 localDelta, Vector2Int direction)
        {
            if (direction.x != 0)
            {
                return localDelta.x * direction.x;
            }

            return localDelta.y * direction.y;
        }

        private BoardCellView FindCellAtScreenPosition(Vector2 screenPosition)
        {
            if (_inputCamera == null)
            {
                ResolveInputCamera();
            }

            if (_inputCamera == null)
            {
                return null;
            }

            float cameraDistance = Mathf.Abs(_inputCamera.transform.position.z - transform.position.z);
            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, cameraDistance);
            Vector3 worldPosition = _inputCamera.ScreenToWorldPoint(screenPoint);
            Collider2D hitCollider = Physics2D.OverlapPoint(worldPosition, _cellLayerMask);

            if (hitCollider == null)
            {
                return null;
            }

            if (_boardGrid == null)
            {
                return null;
            }

            return _boardGrid.GetCell(hitCollider);
        }

        private static PointerFrame ReadPointerFrame()
        {
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;

                if (touch.press.isPressed
                    || touch.press.wasPressedThisFrame
                    || touch.press.wasReleasedThisFrame)
                {
                    return new PointerFrame(
                        true,
                        touch.position.ReadValue(),
                        touch.press.wasPressedThisFrame,
                        touch.press.wasReleasedThisFrame,
                        touch.press.isPressed);
                }
            }

            if (Mouse.current != null)
            {
                bool pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
                bool releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
                bool isPressed = Mouse.current.leftButton.isPressed;

                return new PointerFrame(
                    pressedThisFrame || releasedThisFrame || isPressed,
                    Mouse.current.position.ReadValue(),
                    pressedThisFrame,
                    releasedThisFrame,
                    isPressed);
            }

            return PointerFrame.Empty;
        }

        private readonly struct PointerFrame
        {
            public static readonly PointerFrame Empty = new PointerFrame(false, Vector2.zero, false, false, false);

            public readonly bool HasInput;
            public readonly Vector2 ScreenPosition;
            public readonly bool PressedThisFrame;
            public readonly bool ReleasedThisFrame;
            public readonly bool IsPressed;

            public PointerFrame(
                bool hasInput,
                Vector2 screenPosition,
                bool pressedThisFrame,
                bool releasedThisFrame,
                bool isPressed)
            {
                HasInput = hasInput;
                ScreenPosition = screenPosition;
                PressedThisFrame = pressedThisFrame;
                ReleasedThisFrame = releasedThisFrame;
                IsPressed = isPressed;
            }
        }
    }
}
