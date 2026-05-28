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
        [SerializeField] private Collider2D _hitCollider;

        public int X => _x;
        public int Y => _y;
        public BlockBase Block => _block;
        public Collider2D HitCollider => _hitCollider;

        public void Configure(int x, int y, BoardInputController inputController)
        {
            _x = x;
            _y = y;
            _inputController = inputController;
            gameObject.name = $"Cell_{x}_{y}";

            if (_block == null)
            {
                Debug.LogError("BoardCellView requires a serialized BlockBase reference.", this);
                return;
            }

            _block.SetBoardPosition(new Vector2Int(x, y));
        }
    }
}
