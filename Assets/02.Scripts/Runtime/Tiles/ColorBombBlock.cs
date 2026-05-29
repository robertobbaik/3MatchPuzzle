namespace ThreeMatch.Tiles
{
    public sealed class ColorBombBlock : BlockBase
    {
        public override BlockTypeId TypeId => BlockTypeId.ColorBomb;
        public override bool CanMatch => false;
        public override bool IsItem => true;
        public override bool CanActivate => true;

        public override bool AffectsCell(BlockBase targetBlock, BlockBase candidateBlock, int boardWidth, int boardHeight)
        {
            return candidateBlock == this;
        }
    }
}
