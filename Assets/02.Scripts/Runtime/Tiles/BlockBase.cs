using UnityEngine;

namespace ThreeMatch.Tiles
{
    public abstract class BlockBase : MonoBehaviour
    {
        [SerializeField] private BlockState _state = BlockState.Idle;
        [SerializeField] private Vector2Int _boardPosition;

        public abstract BlockTypeId TypeId { get; }
        public BlockState State => _state;
        public Vector2Int BoardPosition => _boardPosition;

        public virtual bool CanMatch => true;
        public virtual bool CanFall => true;
        public virtual bool CanSwap => true;
        public virtual bool IsItem => false;
        public virtual bool IsObstacle => false;

        public void SetBoardPosition(Vector2Int boardPosition)
        {
            _boardPosition = boardPosition;
        }

        public void SetState(BlockState state)
        {
            _state = state;
        }

        public virtual bool ApplyDamage(int amount)
        {
            return false;
        }
    }
}
