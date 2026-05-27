using UnityEngine;

namespace ThreeMatch.Tiles
{
    public sealed class BoxBlock : BlockBase
    {
        [SerializeField, Min(1)] private int _hp = 1;

        public override BlockTypeId TypeId => BlockTypeId.Box;
        public override bool CanMatch => false;
        public override bool CanFall => false;
        public override bool CanSwap => false;
        public override bool IsObstacle => true;
        public int Hp => _hp;

        public override bool ApplyDamage(int amount)
        {
            if (amount <= 0)
            {
                return _hp <= 0;
            }

            _hp = Mathf.Max(0, _hp - amount);

            return _hp <= 0;
        }
    }
}
