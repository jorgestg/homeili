using Homeili;

Trie trie = new();

trie.Insert("cat"u8);
trie.Insert("car"u8);
trie.Insert("cart"u8);
trie.Insert("catch"u8);
trie.Insert("cab"u8);
trie.Insert("cargo"u8);
trie.Insert("cap"u8);

foreach (TrieNode match in trie.PrefixSearch("ca"u8))
    Console.WriteLine(match.Word);