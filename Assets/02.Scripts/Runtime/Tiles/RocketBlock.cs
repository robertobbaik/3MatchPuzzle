using UnityEngine;

namespace ThreeMatch.Tiles
{
    public sealed class RocketBlock : BlockBase
    {
        [SerializeField] private BlockTypeId _typeId = BlockTypeId.HorizontalRocket;

        public override BlockTypeId TypeId => _typeId;
        public override bool CanMatch => false;
        public override bool IsItem => true;

        private void OnValidate()
        {
            if (!IsRocketType(_typeId))
            {
                _typeId = BlockTypeId.HorizontalRocket;
            }
        }

        private static bool IsRocketType(BlockTypeId typeId)
        {
            return typeId == BlockTypeId.HorizontalRocket
                || typeId == BlockTypeId.VerticalRocket;
        }
    }
}
