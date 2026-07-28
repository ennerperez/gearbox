# Coding Standards

These rules are enforced by [.editorconfig](.editorconfig) and the solution analyzers.
Treat them as non-negotiable unless a local NoWarn is justified and documented.

#### Scope and Objectives

- Runtime: .NET 10.0
- Architecture: DDD (Domain, Application, Infrastructure, Web.Api, Web/Blazor)
- Analyzers: SonarAnalyzer.CSharp; warnings are treated as errors at the solution level
- Use the following configuration to avoid build failures and CI noise

---

### 1) General Editor and Coding

- Indentation: spaces only
- C# files: 4 spaces indentation, tab width 4
- XML/YAML/JSON/TS/CSS: use the indentation type specified below
- Final new line: required for most files; JSON explicitly does NOT end with final new line
- Trim trailing whitespace in all files where enabled
- Encoding for code files: UTF-8 with BOM

Details by file type:
- Project/config XML: 2 spaces
- YAML: 2 spaces, final new line, trim trailing spaces
- JSON: 2 spaces, trim trailing spaces, no final new line
- TypeScript/TSX: 4 spaces, final new line, trim trailing spaces
- CSS/SCSS/SASS/LESS: 4 spaces, final new line, trim trailing spaces

---

### 2) Usings and Namespaces

- Always remove unused using directives (IDE0005: error)
- Place System using directives first; sort using directives consistently
- Place using directives outside of namespace (warning if not)
- Use file-scoped namespaces (IDE0161: error)
    - Do not use block-scoped namespaces even if there's a style entry; the diagnostic takes priority

---

### 3) Accessibility and Modifiers

- Always specify accessibility on members that are not interfaces (error)
- Modifier order: public, private, protected, internal, static, extern, new, virtual, abstract, sealed, override, readonly, unsafe, volatile, async
- Prefer readonly fields when possible
- Primary constructors not allowed (error)

---

### 4) Naming

- Constants: PascalCase (error)
- Private static fields: s_PascalCase or s_camelCase with s_ prefix (error; configured capitalization is camel with s_ prefix)
- Private instance fields: _camelCase with leading underscore (error)
- Async methods: must end with Async (policy defined; severity not applied—still follow it)

---

### 5) This. Qualification

- Fields: do not use this. (warning if you do)
- Properties: do use this. (silent preference, follow for consistency)
- Methods/events: do not use this.

---

### 6) Type Keywords and var

- Use language keywords for types (int, string, etc.)
- Do not use var by default:
    - csharp_style_var_for_built_in_types = false
    - csharp_style_var_when_type_is_apparent = false
    - csharp_style_var_elsewhere = false

---

### 7) Braces and Expression-bodied Members

- Always use braces (csharp_prefer_braces = true, elevated by analyzers)
- Expression-bodied members:
    - Allowed for properties, indexers, accessors
    - Not preferred for methods, constructors, operators
- Prefer local functions over anonymous functions (error)
- Prefer method group conversion when reasonable

---

### 8) Pattern Matching and Null Handling

- Prefer pattern matching over is+cast and as+null check
- Prefer throw expressions and conditional delegate calls
- Use null propagation (?.) and null coalescing (??) when appropriate
- Prefer simplified boolean expressions and compound assignments

---

### 9) Formatting: New Lines, Indentation, Spaces

New lines:
- New line before opening brace: in all constructs
- New line before else/catch/finally
- In anonymous types: place members on new lines
- Between query clauses: maintain new lines
- In object initializers: maintain members on the same line (tooling warning annotated)

Indentation:
- Do not indent braces
- Indent case content; do not indent additionally when using blocks
- Indent switch labels
- Labels aligned to the left

Spaces:
- No space after type conversions (casts)
- Space after flow control keywords (if, for, while, etc.)
- No spaces within parameter list parentheses
- Space before/after binary operators
- Space around colons in inheritance clauses
- No space between method name and opening parenthesis
- No spaces for empty parameter lists

Single line preservation:
- Preserve single-line statements and blocks when already formatted as such

Using statements:
- Prefer simple using statement (using var x = ...)

---

### 10) Attribute Placement

- Do not place attributes on the same line as their targets:
    - Fields, methods, records, types and general attributes each on their own line

---

### 11) Diagnostic Policy (Highlights)

Applied (not exhaustive):
- IDE0005 Remove unnecessary using: error
- IDE0011 Add braces: warning (combined with global braces preference → treat as mandatory)
- IDE0032 Use auto-implemented property: warning
- IDE0034 Simplify default: warning
- IDE0035 Remove unreachable code: warning
- IDE0049 Use language keywords: warning
- IDE0050 Convert anonymous type to tuple: warning
- IDE0051 Remove unused private member: warning
- IDE0055 Formatting: warning
- IDE0060 Remove unused parameter: warning
- IDE0070 Use HashCode.Combine: warning
- IDE0071 Simplify interpolation: warning
- IDE0082 Convert typeof to nameof: warning
- IDE0290 Primary constructors: error
- CA1416 Platform compatibility: warning
- CA1508 Avoid dead conditional code: warning
- CA1859 Prefer concrete types: warning
- CA1860 Prefer Count == 0 over Any(): warning
- RCS1234 Enum duplicate value: warning
- ReSharper: "invert if" marked as error; others per suggested/warning indications

Suppressed or relaxed (selection):
- Many IDE cleanups of "remove unnecessary" in locals/expressions are disabled to avoid unnecessary changes
- Public API XML documentation requirement disabled (CS1591 none), but several RCS11xx rules require adding summaries/params/typeparam (warnings)
- CA1062 null checks disabled (handled by language/runtime)
- ConfigureAwait and xUnit omission rules suppressed to avoid automatic changes

Note:
- [.editorconfig](.editorconfig) explicitly warns that dotnet format may introduce unwanted changes; review diffs before committing
- The solution treats warnings as errors; even warning-level rules can fail CI. Fix or justify with specific NoWarn.

---

### 12) Project/Domain Guide

- Respect DDD boundaries:
    - Domain: pure model, no infrastructure dependencies
    - Application: use cases, orchestrates the domain
    - Infrastructure: EF Core, external services, configurations
    - Web.Api: ASP.NET Core endpoints
    - Web: Blazor UI
- Maintain dependencies pointing inward: Domain <- Application <- Infrastructure/Web.Api/Web

---

### 13) EF Core and Schemas (Convention Guidelines)

- Use the provided Schema constants (Migration, Identity, Default)
- Combine with RelationalEntityTypeBuilderExtensions.ToTable(...) for standardized table names
- Provider selection via DbContextOptionsBuilder extensions; build flags control code paths

---

### 14) Git Hygiene

- No trailing whitespace on lines
- Respect final new line policies by file type
- Keep solution.lock.json under version control for dotnet tool version stability
- Before pushing, ensure no unused using directives and analyzers are clean

---

### 15) Blazor/Frontend Notes

- TS/TSX/CSS/SCSS follow 4 spaces indentation, final new line, trimmed spaces
- Maintain consistent using directives and C# rules in .razor.cs files
- In .razor, align C# code with the same style (braces, names, usings) in code-behind

---

### 16) Quick Checklist

- [ ] File-scoped namespace at the beginning
- [ ] System using first; no unused using directives
- [ ] Accessibility explicitly set
- [ ] Private fields start with `_`; private static fields start with `s_`
- [ ] Constants in PascalCase
- [ ] Async methods end with Async
- [ ] No var (unless later adjusted by analyzer)
- [ ] Braces around all blocks
- [ ] Spaces/new lines according to previous rules
- [ ] No trailing spaces; final new line as required
- [ ] No primary constructors
- [ ] Analyzer warnings resolved or suppressed locally with justification

---

### 17) Tool Warnings

- dotnet format: may apply incorrect or unwanted fixes; review each change
- ReSharper: respects attribute line placement and certain inspections; align with [.editorconfig](.editorconfig)
- If a rule conflicts, error-level diagnostics take priority

Follow this document along with the applied [.editorconfig](.editorconfig) to maintain consistent codebase and clean CI.