using UnityEngine;
using ThreeMatch.Tiles;

namespace ThreeMatch.Board
{
    public sealed class BoardInputController : MonoBehaviour
    {
        [SerializeField] private BoardGrid _boardGrid;

        private BoardCellView _selectedCell;

        public BoardGrid BoardGrid => _boardGrid;
        public BoardCellView SelectedCell => _selectedCell;

        private void Awake()
        {
            ResolveBoardGrid();
        }

        private void OnValidate()
        {
            ResolveBoardGrid();
        }

        public void HandleCellPressed(BoardCellView cell)
        {
            if (cell == null || cell.Block == null || !cell.Block.CanSwap)
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

        private void ResolveBoardGrid()
        {
            if (_boardGrid != null)
            {
                return;
            }

            _boardGrid = GetComponent<BoardGrid>();
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

        private static bool AreAdjacent(BoardCellView firstCell, BoardCellView secondCell)
        {
            int distanceX = Mathf.Abs(firstCell.X - secondCell.X);
            int distanceY = Mathf.Abs(firstCell.Y - secondCell.Y);

            return distanceX + distanceY == 1;
        }

        private void RequestSwap(BoardCellView firstCell, BoardCellView secondCell)
        {
            Debug.Log(
                $"Swap requested: ({firstCell.X}, {firstCell.Y}) -> ({secondCell.X}, {secondCell.Y})",
                this);
        }
    }
}
