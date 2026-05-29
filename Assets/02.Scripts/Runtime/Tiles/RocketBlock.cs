using UnityEngine;

namespace ThreeMatch.Tiles
{
    public sealed class RocketBlock : BlockBase
    {
        [SerializeField] private BlockTypeId _typeId = BlockTypeId.HorizontalRocket;

        public override BlockTypeId TypeId => IsRocketType(_typeId) ? _typeId : BlockTypeId.HorizontalRocket;
        public override bool CanMatch => false;
        public override bool IsItem => true;
        public override bool CanActivate => true;

        private static bool IsRocketType(BlockTypeId typeId)
        {
            return typeId == BlockTypeId.HorizontalRocket
                || typeId == BlockTypeId.VerticalRocket;
        }

        public override bool AffectsCell(BlockBase targetBlock, BlockBase candidateBlock, int boardWidth, int boardHeight)
        {
            Vector2Int candidatePosition = candidateBlock.BoardPosition;

            if (TypeId == BlockTypeId.HorizontalRocket)
            {
                return candidatePosition.y == BoardPosition.y;
            }

            return candidatePosition.x == BoardPosition.x;
        }
    }
}
