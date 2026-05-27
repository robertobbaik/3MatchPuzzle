namespace ThreeMatch.Tiles
{
    public sealed class BombBlock : BlockBase
    {
        public override BlockTypeId TypeId => BlockTypeId.Bomb;
        public override bool CanMatch => false;
        public override bool IsItem => true;
    }
}
