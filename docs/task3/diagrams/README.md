# Task 3 diagram sources

Use `reversi-object.svg` and `save-load-sequence.svg` for the report. SVG preserves readable text when enlarged. The `.mmd` files are editable Mermaid alternatives; the `.py` file reproduces the checked-in SVG layouts using only Python's standard library.

The object diagram is a runtime snapshot, not a class diagram. Underlined names identify actual instance types; slash-prefixed fields are derived values. The board symbols are a readable projection of `Board._cells`. The sequence diagram uses notes for internal calls whose receivers are not separate lifelines, and its Game lifeline refers to the original game in the SAVE panel and the reconstructed game in the LOAD panel.

Regenerate the SVG files:

```powershell
python docs/task3/diagrams/generate_diagrams.py
```

PNG files are generated QA previews. They can also be used if the report editor cannot import SVG. `render_previews.cjs` takes the directory containing the installed `sharp` package as its first argument; no runtime package is added to the C# solution.
