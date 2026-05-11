namespace DockCatWin.Core.StateMachine;

public enum CatStateKind
{
    Transitioning,
    Walking,
    Resting,
    Dragged,
    OutingAsking,
    OutingConfirmingDeparture,
    OutingLeaving,
    OutingAway,
    OutingReturning,
    OutingReturned
}

public readonly record struct CatState(CatStateKind Kind)
{
    public static CatState Transitioning { get; } = new(CatStateKind.Transitioning);
    public static CatState Walking { get; } = new(CatStateKind.Walking);
    public static CatState Resting { get; } = new(CatStateKind.Resting);
    public static CatState Dragged { get; } = new(CatStateKind.Dragged);
    public static CatState OutingAsking { get; } = new(CatStateKind.OutingAsking);
    public static CatState OutingConfirmingDeparture { get; } = new(CatStateKind.OutingConfirmingDeparture);
    public static CatState OutingLeaving { get; } = new(CatStateKind.OutingLeaving);
    public static CatState OutingAway { get; } = new(CatStateKind.OutingAway);
    public static CatState OutingReturning { get; } = new(CatStateKind.OutingReturning);
    public static CatState OutingReturned { get; } = new(CatStateKind.OutingReturned);

    public bool IsLongDuration => Kind is CatStateKind.Walking or CatStateKind.Resting;

    public bool CanBeginDrag => Kind is CatStateKind.Walking or CatStateKind.Resting or CatStateKind.Transitioning;

    public bool IsOuting => Kind is CatStateKind.OutingAsking
        or CatStateKind.OutingConfirmingDeparture
        or CatStateKind.OutingLeaving
        or CatStateKind.OutingAway
        or CatStateKind.OutingReturning
        or CatStateKind.OutingReturned;
}
