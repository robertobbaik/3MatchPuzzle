using UnityEngine;

namespace ThreeMatch.Tiles
{
    public sealed class ColorBlock : BlockBase
    {
        [SerializeField] private BlockTypeId _typeId = BlockTypeId.Red;

        public override BlockTypeId TypeId => IsColorType(_typeId) ? _typeId : BlockTypeId.Red;

        private static bool IsColorType(BlockTypeId typeId)
        {
            return typeId == BlockTypeId.Red
                || typeId == BlockTypeId.Blue
                || typeId == BlockTypeId.Green
                || typeId == BlockTypeId.Yellow
                || typeId == BlockTypeId.Purple;
        }
    }
}
