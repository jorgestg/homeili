using System.Diagnostics.CodeAnalysis;

namespace Homeili;

public interface ITransition
{
    char Label { get; }
    IState Target { get; }
}

public sealed class ImmutableTransition(char label, ImmutableState? target = null) 
    : ITransition, IComparable<ImmutableTransition>, IEquatable<ImmutableTransition>
{
    public char Label { get; } = label;
    public ImmutableState Target { get; } = target ?? new();

    IState ITransition.Target => Target;

    public int CompareTo(ImmutableTransition? other) => TransitionComparer.Instance.Compare(this, other);

    public bool Equals(ImmutableTransition? other) => TransitionComparer.Instance.Equals(this, other);

    public override bool Equals(object? obj) => Equals(obj as ImmutableTransition);

    public override int GetHashCode() => TransitionComparer.Instance.GetHashCode(this);

    public override string ToString() => $"-> {Label}";
}

public sealed class MutableTransition(char label, MutableState target)
    : ITransition, IComparable<MutableTransition>, IEquatable<MutableTransition>
{
    public MutableTransition(ImmutableTransition original)
        : this(original.Label, new MutableState(original.Target)) { }

    public char Label { get; } = label;
    public MutableState Target { get; set; } = target;

    IState ITransition.Target => Target;

    public ImmutableTransition ToImmutable() => new(Label, Target.ToImmutable());

    public int CompareTo(MutableTransition? other) => TransitionComparer.Instance.Compare(this, other);

    public bool Equals(MutableTransition? other) => TransitionComparer.Instance.Equals(this, other);

    public override bool Equals(object? obj) => Equals(obj as MutableTransition);

    public override int GetHashCode() => TransitionComparer.Instance.GetHashCode(this);

    public override string ToString() => $"-> {Label}";
}

public sealed class TransitionComparer : IComparer<ITransition>, IEqualityComparer<ITransition>
{
    public static readonly TransitionComparer Instance = new();

    public int Compare(ITransition? x, ITransition? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        int result = x.Label.CompareTo(y.Label);
        if (result != 0)
            return result;

        return StateComparer.Instance.Compare(x.Target, y.Target);
    }

    public bool Equals(ITransition? x, ITransition? y) => Compare(x, y) == 0;

    public int GetHashCode([DisallowNull] ITransition transition)
    {
        HashCode hashCode = new();
        hashCode.Add(transition.Label.GetHashCode());
        hashCode.Add(transition.Target.GetHashCode());
        return hashCode.ToHashCode();
    }
}
