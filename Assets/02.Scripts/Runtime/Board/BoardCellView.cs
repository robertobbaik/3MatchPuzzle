using UnityEngine;
using ThreeMatch.Tiles;

namespace ThreeMatch.Board
{
    public sealed class BoardCellView : MonoBehaviour
    {
        [SerializeField] private int _x;
        [SerializeField] private int _y;
        [SerializeField] private BlockBase _block;
        [SerializeField] private BoardInputController _inputController;

        public int X => _x;
        public int Y => _y;
        public BlockBase Block => _block;

        public void Configure(int x, int y, BoardInputController inputController)
        {
            _x = x;
            _y = y;
            _inputController = inputController;
            gameObject.name = $"Cell_{x}_{y}";

            ResolveBlock();

            if (_block == null)
            {
                Debug.LogError("BoardCellView requires a BlockBase component in itself or its children.", this);
                return;
            }

            _block.SetBoardPosition(new Vector2Int(x, y));
        }

        private void OnMouseDown()
        {
            if (_inputController == null)
            {
                return;
            }

            _inputController.HandleCellPressed(this);
        }

        private void OnValidate()
        {
            ResolveBlock();
        }

        private void ResolveBlock()
        {
            if (_block != null)
            {
                return;
            }

            _block = GetComponentInChildren<BlockBase>();
        }
    }
}
