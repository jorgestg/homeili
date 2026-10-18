using System.Diagnostics.CodeAnalysis;

namespace Homeili;

public interface ITransition
{
    byte Label { get; }
    IState Target { get; }
    int Output { get; }
}

public sealed class ImmutableTransition(byte label, ImmutableState target, int output)
    : ITransition,
        IComparable<ImmutableTransition>,
        IEquatable<ImmutableTransition>
{
    public ImmutableTransition(byte label, int output)
        : this(label, new ImmutableState(), output) { }

    public byte Label { get; } = label;
    public ImmutableState Target { get; } = target;
    public int Output { get; } = output;

    IState ITransition.Target => Target;

    public int CompareTo(ImmutableTransition? other) =>
        TransitionComparer.Instance.Compare(this, other);

    public bool Equals(ImmutableTransition? other) =>
        TransitionComparer.Instance.Equals(this, other);

    public override bool Equals(object? obj) => Equals(obj as ImmutableTransition);

    public override int GetHashCode() => TransitionComparer.Instance.GetHashCode(this);

    public override string ToString() => $"-> {Label}";
}

public sealed class MutableTransition(byte label, MutableState target, int output)
    : ITransition,
        IComparable<MutableTransition>,
        IEquatable<MutableTransition>
{
    public MutableTransition(ImmutableTransition original)
        : this(original.Label, new MutableState(original.Target), original.Output) { }

    public byte Label { get; } = label;
    public int Output { get; set; } = output;
    public MutableState Target { get; set; } = target;

    IState ITransition.Target => Target;

    public int CompareTo(MutableTransition? other) =>
        TransitionComparer.Instance.Compare(this, other);

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

        result = StateComparer.Instance.Compare(x.Target, y.Target);
        if (result != 0)
            return result;

        return x.Output.CompareTo(y.Output);
    }

    public bool Equals(ITransition? x, ITransition? y) => Compare(x, y) == 0;

    public int GetHashCode([DisallowNull] ITransition transition)
    {
        HashCode hashCode = new();
        hashCode.Add(transition.Label.GetHashCode());
        hashCode.Add(transition.Target.GetHashCode());
        hashCode.Add(transition.Output.GetHashCode());
        return hashCode.ToHashCode();
    }
}
