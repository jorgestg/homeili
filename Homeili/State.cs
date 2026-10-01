using System.Collections.Immutable;

namespace Homeili;

public interface IState
{
    bool IsTerminal { get; }
    IEnumerable<ITransition> Transitions { get; }
}

public sealed class ImmutableState(bool isTerminal, ImmutableArray<ImmutableTransition> transitions)
    : IState, IComparable<ImmutableState>, IEquatable<ImmutableState>
{
    public ImmutableState()
        : this(false, []) { }

    public bool IsTerminal { get; } = isTerminal;
    public ImmutableArray<ImmutableTransition> Transitions { get; } = transitions;

    IEnumerable<ITransition> IState.Transitions => Transitions.Select(t => (ITransition)t);

    public int CompareTo(ImmutableState? other) => StateComparer.Instance.Compare(this, other);

    public bool Equals(ImmutableState? other) => StateComparer.Instance.Equals(this, other);

    public override bool Equals(object? obj) => Equals(obj as ImmutableState);

    public override int GetHashCode() => StateComparer.Instance.GetHashCode(this);

    public override string ToString() =>
        $"{{IsTerminal={IsTerminal}, Transitions=[{string.Join(", ", Transitions)}]}}";
}

public sealed class MutableState : IState, IComparable<MutableState>, IEquatable<MutableState>
{
    public MutableState(ImmutableState state)
    {
        IsTerminal = state.IsTerminal;
        Transitions = [.. state.Transitions.Select(t => new MutableTransition(t))];
    }

    public MutableState()
    {
        Transitions = [];
    }

    public bool IsTerminal { get; set; }
    public SortedSet<MutableTransition> Transitions { get; }

    IEnumerable<ITransition> IState.Transitions => Transitions.Cast<ITransition>();

    public ImmutableState ToImmutable()
    {
        return new ImmutableState(
            IsTerminal,
            [.. Transitions.Select(transition => transition.ToImmutable())]
        );
    }

    public int CompareTo(MutableState? other) => StateComparer.Instance.Compare(this, other);

    public bool Equals(MutableState? other) => StateComparer.Instance.Equals(this, other);

    public override bool Equals(object? obj) => Equals(obj as MutableState);

    public override int GetHashCode() => StateComparer.Instance.GetHashCode(this);

    public override string ToString() =>
        $"{{IsTerminal={IsTerminal}, Transitions=[{string.Join(", ", Transitions)}]}}";
}

public sealed class StateComparer : IComparer<IState>, IEqualityComparer<IState>
{
    public static readonly StateComparer Instance = new();

    public int Compare(IState? x, IState? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        int result = x.IsTerminal.CompareTo(y.IsTerminal);
        if (result != 0)
            return result;

        foreach (var (a, b) in x.Transitions.Zip(y.Transitions))
        {
            result = TransitionComparer.Instance.Compare(a, b);
            if (result != 0)
                return result;
        }

        return x.Transitions.Count().CompareTo(y.Transitions.Count());
    }

    public bool Equals(IState? x, IState? y) => Compare(x, y) == 0;

    public int GetHashCode(IState state)
    {
        HashCode hashCode = new();
        hashCode.Add(state.IsTerminal.GetHashCode());
        foreach (ITransition transition in state.Transitions)
            hashCode.Add(transition.GetHashCode());

        return hashCode.ToHashCode();
    }
}
