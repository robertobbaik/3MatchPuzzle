namespace ThreeMatch.Tiles
{
    public enum BlockTypeId
    {
        None = 0,
        Red = 100,
        Blue = 101,
        Green = 102,
        Yellow = 103,
        Purple = 104,
        HorizontalRocket = 200,
        VerticalRocket = 201,
        Bomb = 202,
        ColorBomb = 203,
        Box = 300
    }

    public enum BlockState
    {
        Idle,
        Selected,
        Swapping,
        Matched,
        Falling,
        Spawning,
        Locked
    }
}
