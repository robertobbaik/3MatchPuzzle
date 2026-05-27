namespace ThreeMatch.Tiles
{
    public sealed class ColorBombBlock : BlockBase
    {
        public override BlockTypeId TypeId => BlockTypeId.ColorBomb;
        public override bool CanMatch => false;
        public override bool IsItem => true;
    }
}
