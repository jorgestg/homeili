# Homeili

Project to learn how search engines work, by implementing Meilisearch internals.
Also, make use of AI for writing tests -and possibly tooling- but NOT the implementation itself.

## Playground

`dotnet run Playground.cs`

## Stuff to learn

- Inverted index
- FSTs for dictionaries
- Roaring bitmaps
- Ranking algorithm:
    1. Matches all terms
    2. Typo count
    3. Proximity
    4. Attribute weight
    5. Prefix/exact
    6. Exact match
- Filtering
- Faceting
- Storage mechanisms

## Learning path

- [x] Trie
- [x] DFA
- [ ] FST
- [ ] Damas-Levenshtein
- [ ] Roaring bitmaps

## Sources

- [What is full-text search and how does it work? - Meilisearch Blog](https://www.meilisearch.com/blog/how-full-text-search-engines-work)