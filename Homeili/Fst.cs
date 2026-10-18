using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Homeili;

public sealed class Fst
{
    private ImmutableState _root = new();
    private MutableState? _mutableRoot;

    [MemberNotNullWhen(true, nameof(_mutableRoot))]
    private bool Dirty { get; set; }

    public void Insert(ReadOnlySpan<byte> term, int output)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(output, term.Length);

        _mutableRoot ??= new(_root);

        SearchResult searchResult = Search(_mutableRoot, term);
        if (searchResult.IsExactMatch)
        {
            if (!searchResult.ExactState.IsTerminal)
            {
                Dirty = true;
                ((MutableState)searchResult.ExactState).IsTerminal = true;
            }

            return;
        }

        Dirty = true;

        MutableState state = (MutableState)searchResult.ClosestState;
        MutableTransition transition = null!;
        int outputPerTransition = (int)
            Math.Floor((double)output / searchResult.UnmatchedSuffix.Length);

        foreach (byte label in searchResult.UnmatchedSuffix)
        {
            transition = new(label, new MutableState(), outputPerTransition);
            state.Transitions.Add(transition);
            state = transition.Target;
        }

        state.IsTerminal = true;
        if (outputPerTransition % output != 0)
            transition.Output += output % outputPerTransition;
    }

    public bool Contains(ReadOnlySpan<byte> term)
    {
        Minimize();

        SearchResult searchResult = Search(_root, term);
        if (searchResult.IsExactMatch)
            return searchResult.ExactState.IsTerminal;

        return false;
    }

    public List<int> PrefixSearch(ReadOnlySpan<byte> prefix, int limit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        Minimize();

        List<int> hits = [];
        SearchResult searchResult = Search(_root, prefix);
        if (!searchResult.IsExactMatch)
            return hits;

        int path = 0;
        if (searchResult.ExactState.IsTerminal)
            hits.Add(searchResult.AccumulatedOutput);

        CollectTerminalsRecursively((ImmutableState)searchResult.ExactState, path, hits, limit);
        return hits;

        static void CollectTerminalsRecursively(
            ImmutableState parent,
            int outputAccumulator,
            List<int> hits,
            int limit
        )
        {
            foreach (ImmutableTransition child in parent.Transitions)
            {
                if (hits.Count == limit)
                    return;

                outputAccumulator += child.Output;

                if (child.Target.IsTerminal)
                    hits.Add(outputAccumulator);

                CollectTerminalsRecursively(child.Target, outputAccumulator, hits, limit);
                outputAccumulator -= child.Output;
            }
        }
    }

    private void Minimize()
    {
        if (!Dirty)
            return;

        MutableState newRoot = MinimizeRecursively(_mutableRoot, registry: []);
        _root = FreezeRecursively(newRoot, registry: []);
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

        static ImmutableState FreezeRecursively(
            MutableState mutableState,
            Dictionary<MutableState, ImmutableState> registry
        )
        {
            if (registry.TryGetValue(mutableState, out ImmutableState? existing))
                return existing;

            var transitions = ImmutableArray.CreateBuilder<ImmutableTransition>(
                mutableState.Transitions.Count
            );

            foreach (MutableTransition t in mutableState.Transitions)
            {
                ImmutableState frozenState = FreezeRecursively(t.Target, registry);
                transitions.Add(new ImmutableTransition(t.Label, frozenState, t.Output));
            }

            ImmutableState immutableState = new(mutableState.IsTerminal, transitions.MoveToImmutable());
            registry.Add(mutableState, immutableState);
            return immutableState;
        }
    }

    private readonly ref struct SearchResult
    {
        [MemberNotNullWhen(true, nameof(ExactTransition))]
        [MemberNotNullWhen(true, nameof(ExactState))]
        [MemberNotNullWhen(false, nameof(ClosestState))]
        public bool IsExactMatch { get; init; }

        public IState? ExactState { get; init; }
        public ITransition? ExactTransition { get; init; }

        public IState? ClosestState { get; init; }
        public required int AccumulatedOutput { get; init; }
        public required ReadOnlySpan<byte> UnmatchedSuffix { get; init; }
    }

    private static SearchResult Search(IState root, ReadOnlySpan<byte> term)
    {
        IState closestState = root;
        ITransition? exactTransition = null;
        int accumulatedOutput = 0;
        ReadOnlySpan<byte> unmatchedSuffix = term;

        foreach (byte label in term)
        {
            exactTransition = closestState.Transitions.FirstOrDefault(t => t.Label == label);
            if (exactTransition == null)
            {
                return new SearchResult
                {
                    IsExactMatch = false,
                    ClosestState = closestState,
                    AccumulatedOutput = accumulatedOutput,
                    UnmatchedSuffix = unmatchedSuffix,
                };
            }

            closestState = exactTransition.Target;
            accumulatedOutput += exactTransition.Output;
            unmatchedSuffix = unmatchedSuffix[1..];
        }

        return new SearchResult
        {
            IsExactMatch = true,
            ExactState = closestState,
            ExactTransition = exactTransition,
            AccumulatedOutput = accumulatedOutput,
            UnmatchedSuffix = unmatchedSuffix,
        };
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
