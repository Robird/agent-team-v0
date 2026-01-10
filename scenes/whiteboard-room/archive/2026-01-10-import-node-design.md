# Design Discussion: Import-Node for AI-Design-DSL
Date: 2026-01-10
Scene: Whiteboard Room
Participants: Impresario, Investigator, DocOps
Goal: Design the `Import-Node` mechanism.

### The Opening
**Impresario**: (Writes "Import-Node" on the whiteboard and circles it) "Alright, team. Craftsman wants to break the silos. Our Design DSL is great for single docs, but we need to reference terms across the project. How do we design the bridge?"

### On Granularity
**Investigator**: "I've reviewed the current spec. Currently, `Term-Node`s are resolved locally. For cross-doc, we have two paths:
1. **Cherry-Pick**: Like ES Modules `import { TermA } from './DocA'`. Precise, keeps scopes clean.
2. **Whole-File**: Like C `include` or C# `using`. You get everything."

**DocOps**: (Frowning) "Cherry-picking is a maintenance nightmare for docs. If I rename a term in the source, I have to grep every consumer. With Whole-File, I just import the 'Glossary' and I'm good."

**Investigator**: "Agreed. Also, from a parsing perspective, Whole-File is just 'load the dependency graph'. Cherry-picking requires parsing the destination deeper to validate."

**Impresario**: (Draws a big box labeled 'File' being dropped into a bucket) "So, Whole-File is the MVP. It fits the 'Lazy Author' persona. We can always add filtering later if pollution becomes an issue."

### On Namespacing & Collisions
**Investigator**: "But what if I import `Network-Spec` and `Database-Spec` and both define `Latency`? Who wins?"

**DocOps**: "In a well-governed project, `Latency` should be defined once, or specifically as `Network-Latency` and `Database-Latency`. We are designing a DSL for *Design*, not general purpose coding. Ambiguity is the enemy."

**Investigator**: "So... Flat by default? Direct merge into the symbol table?"

**DocOps**: "Yes. It forces better naming. But... we might need a safety valve. If I'm importing a 3rd party spec (read-only), I can't rename their terms."

**Impresario**: "Good point. So: **Flat Default**, with **Optional Aliasing** for the file scope."

### On Syntax
**Investigator**: "Currently ATX Nodes are: `### keyword [ID] Title`.
How about `### import [Namespace] (Path)`?"

**DocOps**: "Why square brackets for Namespace? That usually means `Clause-ID`. Importing is an action, not exactly a definition of a term."

**Investigator**: "Right. And we need to support paths. Markdown links work like `[text](url)`.
Maybe reuse that? `### import [Local-Alias](path/to/file.md)`."

**DocOps**: "If I don't want an alias? `### import [](path/to/file.md)`? Ugly."

**Investigator**: "How about we treat it like a `derived` node? Or just a new keyword."
"Let's look at `spec [F-TERM-REFERENCE-FORMAT]`. It uses `@Term`.
Maybe `### import path/to/file.md` is enough if we don't alias?"

**Impresario**: (Sketches options)
1. `### import ./docs/glossary.md`
2. `### import [Glossary] ./docs/glossary.md` (Square brackets for optional ID/Alias)

**DocOps**: "I like option 2. Matches `[Clause-ID]` syntax visually. If the Alias is present, we prefix. If not, we merge."

**Investigator**: "Wait, if we prefix, how do we reference it? `@Glossary.Term-A`? Our `Term-ID` spec `[F-TERM-ID-FORMAT]` forbids dots."

**DocOps**: "We'd need to amend `[F-TERM-ID-FORMAT]` or use a different separator. Or maybe... the Alias just renames the ambiguous terms locally? No, that's too magic."

**Investigator**: "Let's keep it simple. If you use Alias, the term becomes `Alias-Term-A` (Hyphenated). It stays compliant with Kebab-Case."

**Impresario**: "Ooh, `Glossary-Term-A`. Natural. I like it."

### Conclusion
**Impresario**: "Let's summarize.
1. **Node**: `Import-Node` via `### import`.
2. **Path**: Relative path to target markdown file.
3. **Mode**: Whole-file inclusion.
4. **Namespace**: Flat by default (Direct Merge). Optional Alias via `[Alias]` which prefixes with hyphen."

**DocOps**: "One more thing: Cycles. A imports B, B imports A."
**Investigator**: "Parser detects cycles and ignores re-imports. Standard practice."
