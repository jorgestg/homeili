using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;

namespace Homeili;

public sealed class Fst
{
    private ImmutableState _root = new();
    private MutableState? _mutableRoot;

    [MemberNotNullWhen(true, nameof(_mutableRoot))]
    private bool Dirty { get; set; }

    public void Insert(string term)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(term);

        term = term.Trim();

        _mutableRoot ??= new(_root);
        if (Search(_mutableRoot, term, out IState closest, out ReadOnlySpan<char> unmatched))
        {
            if (!closest.IsTerminal)
            {
                Dirty = true;
                ((MutableState)closest).IsTerminal = true;
            }

            return;
        }

        Dirty = true;

        MutableState mutableClosest = (MutableState)closest;
        foreach (char label in unmatched)
        {
            MutableTransition t = new(label, new MutableState());
            mutableClosest.Transitions.Add(t);
            mutableClosest = t.Target;
        }

        mutableClosest.IsTerminal = true;
    }

    public bool Contains(string term)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(term);

        Minimize();

        term = term.Trim();
        if (Search(_root, term, out IState closest, out ReadOnlySpan<char> _))
            return closest.IsTerminal;

        return false;
    }

    public List<string> PrefixSearch(string prefix, int limit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        Minimize();

        List<string> hits = [];
        prefix = prefix.Trim();
        if (!Search(_root, prefix, out IState closest, out _))
            return hits;

        List<char> path = [];
        path.AddRange(prefix);
        if (closest.IsTerminal)
            hits.Add(prefix);

        CollectRecursively((ImmutableState)closest, path, hits, limit);
        return hits;

        static void CollectRecursively(ImmutableState parent, List<char> path, List<string> hits, int limit)
        {
            foreach (ImmutableTransition child in parent.Transitions)
            {
                if (hits.Count == limit)
                    return;

                path.Add(child.Label);

                if (child.Target.IsTerminal)
                    hits.Add(string.Join(string.Empty, path));

                CollectRecursively(child.Target, path, hits, limit);
                path.RemoveAt(path.Count - 1);
            }
        }
    }

    private void Minimize()
    {
        if (!Dirty)
            return;

        MutableState newRoot = MinimizeRecursively(_mutableRoot, registry: []);
        _root = newRoot.ToImmutable();
        _mutableRoot = null;

        Dirty = false;

        static MutableState MinimizeRecursively(MutableState state, HashSet<MutableState> registry)
        {
            foreach (MutableTransition t in state.Transitions)
                t.Target = MinimizeRecursively(t.Target, registry);

            if (registry.TryGetValue(state, out MutableState? existing))
                return existing;

            registry.Add(state);
            return state;
        }
    }

    private static bool Search(
        IState root,
        ReadOnlySpan<char> term,
        out IState closest,
        out ReadOnlySpan<char> unmatched
    )
    {
        closest = root;
        unmatched = term;
        foreach (char label in term)
        {
            ITransition? t = closest.Transitions.FirstOrDefault(t => t.Label == label);
            if (t == null)
                return false;

            closest = t.Target;
            unmatched = unmatched.Slice(1);
        }

        return true;
    }

    public override string ToString()
    {
        IndentedTextWriter writer = new(new StringWriter(), "  ");
        Write(_root, writer);
        return writer.InnerWriter.ToString()!;

        static void Write(ImmutableState state, IndentedTextWriter writer)
        {
            writer.WriteLine('{');
            writer.Indent++;

            writer.Write("\"IsTerminal\": ");
            writer.WriteLine(state.IsTerminal ? "true," : "false,");

            writer.WriteLine("\"Transitions\": [");
            writer.Indent++;

            foreach (ImmutableTransition transition in state.Transitions)
            {
                writer.WriteLine('{');
                writer.Indent++;

                writer.Write("\"Label\": ");
                writer.WriteLine(transition.Label);

                writer.Write("\"Target\": ");
                Write(transition.Target, writer);

                writer.Indent--;
                writer.Write("},");
                writer.WriteLine();
            }

            writer.Indent--;
            writer.WriteLine(']');
            writer.Indent--;
            writer.WriteLine("},");
        }
    }
}
