# Family trees: pictorial tree and diagram tree

Contents
1. Choose the view
2. Data model (Arabic-aware)
3. Architecture
4. Lazy loading and caching for trees
5. Diagram tree layout
6. Pictorial tree layout
7. Rendering and performance tiers
8. RTL rules for both views
9. Interaction
10. Export and print
11. Checklist

## 1. Choose the view

| | Diagram tree | Pictorial tree |
|---|---|---|
| Purpose | Accurate exploring and editing | Display, print, heritage poster or gift |
| Readable size | Thousands (with lazy expansion) | About 20–150 people |
| Look | Boxes in generation rows/columns, orthogonal connectors | Trunk, tapered branches, round portrait medallions |
| Theme | Follows app light/dark | Fixed artistic palette (it's artwork and usually printed) |
| Variants | Descendants, pedigree (ancestors), hourglass (both) | Descendant tree (founder on the trunk), ancestor tree (subject on the trunk) |

Both views share the same data, graph, lazy loading and caching. They differ only in layout and renderer.

## 2. Data model (Arabic-aware)

Model families as unions. A person can belong to several unions at once or over time, which covers multiple marriages (including several concurrent wives), remarriage, half-siblings and unknown partners.

```sql
CREATE TABLE Person (
  Id                 INTEGER PRIMARY KEY,
  GivenName          TEXT NOT NULL,        -- الاسم
  FamilyName         TEXT,                 -- العائلة / القبيلة
  Laqab              TEXT,                 -- اللقب
  Kunya              TEXT,                 -- أبو فلان / أم فلان
  Gender             INTEGER NOT NULL,     -- 0 unknown, 1 male, 2 female
  BirthDate          TEXT,                 -- ISO-8601, Gregorian
  BirthPrecision     INTEGER NOT NULL DEFAULT 0, -- 0 unknown, 1 year, 2 month, 3 day, 4 approximate
  DeathDate          TEXT,
  DeathPrecision     INTEGER NOT NULL DEFAULT 0,
  IsDeceased         INTEGER NOT NULL DEFAULT 0,
  HidePhoto          INTEGER NOT NULL DEFAULT 0,
  NameSearch         TEXT NOT NULL         -- ArabicText.NormalizeForSearch(...)
);

CREATE TABLE FamilyUnion (
  Id          INTEGER PRIMARY KEY,
  Partner1Id  INTEGER REFERENCES Person(Id),
  Partner2Id  INTEGER REFERENCES Person(Id),
  UnionOrder  INTEGER NOT NULL DEFAULT 1,  -- 1st, 2nd... marriage of Partner1
  StartDate   TEXT,
  EndDate     TEXT,
  EndReason   INTEGER                      -- divorce, death, unknown
);

CREATE TABLE ChildOf (
  ChildId     INTEGER NOT NULL REFERENCES Person(Id),
  UnionId     INTEGER NOT NULL REFERENCES FamilyUnion(Id),
  BirthOrder  INTEGER,
  Relation    INTEGER NOT NULL DEFAULT 0,  -- 0 biological, 1 kafala/foster, 2 step, 3 adopted
  PRIMARY KEY (ChildId, UnionId)
);

-- Photos live apart from Person so list and tree queries stay light (lazy loading).
CREATE TABLE PersonPhoto (
  PersonId  INTEGER PRIMARY KEY REFERENCES Person(Id),
  Thumb     BLOB,      -- ~128 px, for tree nodes
  Full      BLOB       -- loaded only in the details view
);

CREATE INDEX IX_Union_P1  ON FamilyUnion(Partner1Id);
CREATE INDEX IX_Union_P2  ON FamilyUnion(Partner2Id);
CREATE INDEX IX_Child_U   ON ChildOf(UnionId);
CREATE INDEX IX_Child_C   ON ChildOf(ChildId);
CREATE INDEX IX_Person_NameSearch_Id ON Person(NameSearch, Id);   -- search + keyset paging
```

**Arabic names.** Store the parts, and compute the nasab (lineage chain) from the tree rather than storing it, so it stays correct after edits. Display modes:
- Short: given name + family name.
- Full nasab: walk the father link (the male partner of the union the person is a child of), joining with `بن` (`بنت` for a daughter's own link), e.g. `فاطمة بنت محمد بن علي آل فلان`.
- With kunya or laqab when present.

**Dates.** Store Gregorian ISO-8601 plus a `DatePrecision` value, because many ancestors only have a year or "approximately". Display every date through `IDateFormatter` (`Services/DateFormatter.cs` in the template). It handles Gregorian or Hijri per the user's setting, and it falls back from Umm al-Qura (which only covers about 1900–2077 CE) to the tabular Hijri calendar for older ancestors. Never invent a day or month the user didn't enter.

**Cultural options.** Keep these configurable, never hard-coded:
- Deceased marker: offer `رحمه الله` / `رحمها الله`, a subtle symbol, or nothing. Don't default to a cross (†).
- Photos: some families prefer not to show certain members' photos. Honor `HidePhoto` with a neutral placeholder (initials or a generic silhouette).
- Lineage view: many Arabic family trees (شجرة العائلة) show the male line only, with daughters shown but their descendants omitted. Offer "male-line descendants" and "all descendants" as options.
- Non-biological links (kafala/foster, step, adopted) render with dashed connectors. A child under kafala keeps their own lineage name, so the nasab walk uses biological links only.
- Gender cues: a small outline icon or text label, never color alone, and no colored node backgrounds.

## 3. Architecture

```
SQLite → FamilyRepository → FamilyGraph (identity map) → ITreeLayout → TreeLayoutResult → Renderer (WPF)
                                                                                         → Exporter (PNG, SVG, print)
```

- The layout engine is pure C#, with no WPF types. It's deterministic, unit-testable and runs inside `Task.Run`.
- Lay out in logical coordinates where x grows from start to end (eldest first). Apply RTL in exactly one place (section 8).
- The result is immutable, so it can be cached and reused by every renderer and exporter.

```csharp
public readonly record struct PointD(double X, double Y);
public readonly record struct RectD(double X, double Y, double W, double H);

public enum EdgeKind { ParentChild, Union, NonBiological }

public readonly record struct NodeBox(long PersonId, RectD Rect, int Generation, bool HasMore, bool IsDuplicate);
public readonly record struct Edge(PointD[] Points, EdgeKind Kind);

public sealed record TreeLayoutResult(IReadOnlyList<NodeBox> Nodes, IReadOnlyList<Edge> Edges, RectD Bounds);

public interface ITreeLayout
{
    TreeLayoutResult Layout(FamilyGraph graph, long rootId, TreeLayoutOptions options);
}
// Implementations: DescendantDiagramLayout, PedigreeDiagramLayout, PictorialRadialLayout
```

## 4. Lazy loading and caching for trees

**Initial load:** load the root plus N generations (3–4 for diagrams; everything within the size limit for pictorial) in one recursive query. Flag frontier people who have unloaded children as `HasMore`, which shows a "+" expander.

```sql
CREATE VIEW PartnerOf AS
  SELECT Partner1Id AS PersonId, Id AS UnionId FROM FamilyUnion WHERE Partner1Id IS NOT NULL
  UNION ALL
  SELECT Partner2Id, Id FROM FamilyUnion WHERE Partner2Id IS NOT NULL;

WITH RECURSIVE d(PersonId, Gen) AS (
  SELECT @rootId, 0
  UNION
  SELECT c.ChildId, d.Gen + 1
  FROM d
  JOIN PartnerOf p ON p.PersonId = d.PersonId
  JOIN ChildOf  c ON c.UnionId  = p.UnionId
  WHERE d.Gen < @maxGen
)
SELECT p.Id, p.GivenName, p.FamilyName, p.Gender, p.BirthDate, p.BirthPrecision,
       p.DeathDate, p.IsDeceased, p.HidePhoto,
       MIN(d.Gen) AS Gen,
       EXISTS (SELECT 1 FROM PartnerOf po JOIN ChildOf c2 ON c2.UnionId = po.UnionId
               WHERE po.PersonId = p.Id) AS HasChildren
FROM d JOIN Person p ON p.Id = d.PersonId
GROUP BY p.Id;
```

Then load the `FamilyUnion` and `ChildOf` rows for the loaded IDs in one more query. Pass the IDs as JSON (`WHERE ... IN (SELECT value FROM json_each(@ids))`) instead of building huge parameter lists. `HasMore = HasChildren && Gen == maxGen`.

**Expanding:** clicking "+" loads two more generations for that person, merges them into the graph, bumps `graph.Version`, and re-runs layout in the background. Keep the clicked node visually still: record its screen position before relayout and adjust the pan offset afterwards so the tree grows around it instead of jumping.

**Identity map and pedigree collapse:** `FamilyGraph.GetOrAdd(id)` guarantees one node per person. Cousin marriages mean the same ancestor is reached by two paths. In ancestor charts, draw the second occurrence as a compact linked box (`IsDuplicate`) that jumps to the original, rather than expanding the whole branch again. Keep a visited set in every traversal so bad data with cycles can't hang the app.

**Caches:**
- Graph: per open tree, owned by the page ViewModel.
- Layout: key `(rootId, viewKind, optionsHash, graph.Version)` → `TreeLayoutResult`. Switching between diagram and pictorial and back is then free.
- Thumbnails: `IMemoryCache` keyed `photo:{personId}:{pixelSize}`, sized by bytes. Decode at displayed size × DPI scale, then `Freeze()`.
- Photos load only when the node intersects the viewport and the zoom level is high enough to show them (level of detail).

## 5. Diagram tree layout

**Visual conventions:**
- Node box about 170×72 DIP: a 40 px photo/initials circle on the start side; the name in up to 2 lines with `TextTrimming="CharacterEllipsis"` and the full nasab in a tooltip; a dates line underneath.
- Couples sit side by side, joined by a short union line. Children drop from the midpoint of their own union's line.
- Multiple partners: place partners in `UnionOrder`, each union with its own group of children directly beneath. Number the unions (١، ٢، ٣) when there are more than two.
- Connectors are orthogonal elbows: down from the union midpoint to a "bus" halfway down the generation gap, across the bus from the first child to the last, then down to each child. Dashed lines mark non-biological links. A 6 px corner radius looks softer.
- Spacing: partner gap 16, sibling gap 20, subtree gap 36, generation gap 64.
- Orientation: descendants top-down; pedigree runs horizontally from the subject on the start side; hourglass puts ancestors above and descendants below the subject.

**Descendant layout:** treat each family unit (a person plus their partners) as one layout node. Its children are the children of all its unions, ordered by `(UnionOrder, BirthOrder)` so each union's children stay together.

```csharp
// Pass 1 (post-order): how wide each subtree needs to be
double Measure(Unit u)
{
    double kids = u.Children.Sum(Measure) + Math.Max(0, u.Children.Count - 1) * opt.SiblingGap;
    u.SubtreeWidth = Math.Max(u.OwnWidth, kids);
    return u.SubtreeWidth;
}

// Pass 2 (pre-order): place each unit centred over its children
void Place(Unit u, double left, int gen)
{
    u.X = left + (u.SubtreeWidth - u.OwnWidth) / 2;
    u.Y = gen * (opt.NodeHeight + opt.GenerationGap);

    double kids = u.Children.Sum(c => c.SubtreeWidth) + Math.Max(0, u.Children.Count - 1) * opt.SiblingGap;
    double x = left + (u.SubtreeWidth - kids) / 2;
    foreach (var c in u.Children)
    {
        Place(c, x, gen + 1);
        x += c.SubtreeWidth + opt.SiblingGap;
    }
}
```

This is the layout to ship. It never overlaps and keeps parents centred. It does waste some space in uneven trees; implement the Buchheim–Jünger–Leipert version of Walker's algorithm (same inputs and outputs, a drop-in `ITreeLayout`) only if the user asks for a more compact chart.

**Pedigree (ancestor) layout:** this is a binary tree, so use slots. Generation `g` (0 = subject) has `2^g` slots. For the person in slot `i`, the father goes in slot `2i` of the next generation and the mother in slot `2i + 1`.

```csharp
double x = g * (opt.NodeWidth + opt.GenerationGap);                       // start → end
double slot = (1 << opt.MaxGenerations) * (opt.NodeHeight + opt.SiblingGap) / (1 << g);
double y = i * slot + (slot - opt.NodeHeight) / 2;
```

Height doubles with every generation, so show 4–5 generations at a time. To go further back, click an ancestor to re-root the chart on them, with breadcrumbs to return. Leave unknown ancestors' slots empty, or show a faint "+ إضافة" placeholder in edit mode.

## 6. Pictorial tree layout

The concept: the founder (or the subject, for an ancestor tree) sits on the trunk. Generations radiate outward on concentric arcs above it. Branches are tapered curves, and each person is a medallion (round portrait plus name plaque).

**Algorithm (radial tidy tree):**
1. Post-order, count leaves: `Leaves(n) = max(1, sum of children's Leaves)`.
2. Give the root an angular sector (e.g. −10° to 190°, a 200° fan; angles counter-clockwise from the right).
3. Split each node's sector among its children in proportion to their leaves. Each node sits at the centre of its sector.
4. Radius = `gen × RingGap`, where `RingGap` ≥ medallion diameter + plaque height + margin. If the outer ring is crowded (arc length per leaf < medallion width), increase `RingGap` or shrink medallions per generation (1.0, 0.9, 0.8...).
5. Position: `x = cx + r·cos θ`, `y = cy − r·sin θ` (screen y points down).

```csharp
void Allocate(Node n, double from, double to, int gen)
{
    n.Angle = (from + to) / 2;
    n.Radius = gen * opt.RingGap;
    double a = from;
    foreach (var c in n.Children)
    {
        double span = (to - from) * c.Leaves / n.Leaves;
        Allocate(c, a, a + span, gen + 1);
        a += span;
    }
}
// RTL (eldest on the right): Allocate(root, -10, 190, 0)
// LTR (eldest on the left):  Allocate(root, 190, -10, 0)
```

**Branches:** draw a cubic Bézier from parent to child. Put control point 1 on the parent's ray, pushed outward by `0.5 × RingGap`, and control point 2 on the child's ray, pulled inward by the same amount. This makes branches look grown rather than like straight spokes. Taper with `thickness = Math.Max(1.5, trunkWidth * Math.Pow(0.7, gen))`. Group branches into one `Path` per generation (one thickness each) to keep the element count low.

**Visual style:**
- Fixed palette with 2–3 presets, independent of the app theme. For example, a flat parchment background `#F4EBD9` (no gradients), trunk and branches `#6B4F35`, leaf accents `#5E7D4E` / `#87A06A`, plaques cream with a thin gold border `#B08D57`.
- Give each child of the root its own branch accent color and pass it down that line, so the eye can follow a lineage.
- Decorative leaves: one shared frozen `Geometry` drawn many times inside a `DrawingGroup`, never one element per leaf.
- Title cartouche with the family name in Aref Ruqaa. Names in Amiri. Both are OFL, bundled in `Assets/Fonts`, and used only by the pictorial view.
- Medallion: circular photo (`Ellipse` filled with an `ImageBrush`, `Stretch="UniformToFill"`), or initials/silhouette when `HidePhoto` is set.
- Keep text horizontal. Never rotate Arabic along branches, because rotated Arabic is hard to read. Use at most 2 lines, and shrink the font per generation but never below about 10 pt at print size.
- Above about 150 people the result becomes unreadable. Offer a generation limit, a single-branch filter (one son's line), or a switch to the diagram view.

## 7. Rendering and performance tiers

| Visible nodes | Approach |
|---|---|
| Up to ~300 | `ItemsControl` with a `Canvas` panel, plus one `Path` for all edges |
| 300–3,000 | Same, plus level of detail (zoomed out: plain colored rectangles, no photos or text), very light templates, photos loaded only inside the viewport |
| 3,000+ | Custom `FrameworkElement` drawing with `DrawingVisual`, rendering only nodes that intersect the viewport, hit-testing via a spatial grid |

```xml
<Grid Width="{Binding Bounds.W}" Height="{Binding Bounds.H}">   <!-- Canvas doesn't size to its children -->
  <Path Data="{Binding EdgeGeometry}" Stroke="{DynamicResource TreeLineBrush}" StrokeThickness="1.5" />
  <ItemsControl ItemsSource="{Binding Nodes}">
    <ItemsControl.ItemsPanel>
      <ItemsPanelTemplate><Canvas /></ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemContainerStyle>
      <Style TargetType="ContentPresenter">
        <Setter Property="Canvas.Left" Value="{Binding X}" />
        <Setter Property="Canvas.Top" Value="{Binding Y}" />
      </Style>
    </ItemsControl.ItemContainerStyle>
  </ItemsControl>
</Grid>
```

Build all edges into one geometry:

```csharp
static Geometry BuildEdges(IEnumerable<Edge> edges)
{
    var g = new StreamGeometry();
    using (var ctx = g.Open())
        foreach (var e in edges)
        {
            ctx.BeginFigure(new Point(e.Points[0].X, e.Points[0].Y), isFilled: false, isClosed: false);
            for (int i = 1; i < e.Points.Length; i++)
                ctx.LineTo(new Point(e.Points[i].X, e.Points[i].Y), isStroked: true, isSmoothJoin: true);
        }
    g.Freeze();   // can then be built off the UI thread and handed over
    return g;
}
```

Put dashed non-biological edges in a second `Path` with `StrokeDashArray`.

Rules:
- Thousands of `Line` elements are the most common tree performance killer. Use one `Path` per stroke style.
- No shadows or effects on nodes: a 1 px card stroke and the card background brush only (as in `ui-ux.md`). Effects also cost an extra render pass each.
- Zoom and pan with `RenderTransform` (`ScaleTransform` + `TranslateTransform`). Never use `LayoutTransform`, which forces a full re-layout.
- During an active pan or zoom on large trees, set `CacheMode = new BitmapCache()` on the content, then clear it afterwards so text re-renders sharply.
- Compute the layout off the UI thread, then apply it in one batch by replacing the `Nodes` collection.

## 8. RTL rules for both views

- The layout engine always works in logical start→end coordinates, eldest first.
- Keep the outer pan/zoom host `FlowDirection="LeftToRight"` so mouse and transform math stays simple, and mirror inside it.
- **Diagram (template-based):** let `FlowDirection="RightToLeft"` on the inner content mirror the canvas. Positions and connectors mirror correctly, and text renders correctly. Set `FlowDirection="LeftToRight"` on photos inside nodes, or faces come out flipped. In RTL, the pedigree subject sits on the right with ancestors extending left.
- **Pictorial:** generate angles start-side first (the `Allocate` comment in section 6) and keep the pictorial canvas LTR, so artwork and photos are never mirrored. Give name `TextBlock`s `FlowDirection="RightToLeft"` explicitly.
- **Custom drawing (`DrawingContext`):** keep the element LTR, mirror x yourself (`x' = bounds.W − x − w`), and create `FormattedText` with `FlowDirection.RightToLeft` for Arabic names. Drawing into a mirrored element would render the text mirrored too.
- **Digits:** on screen, `NumberSubstitution` handles them, but it's render-only. SVG and text exports must convert digits explicitly if Arabic-Indic digits are wanted.

## 9. Interaction

- The mouse wheel zooms around the cursor; dragging pans. Add keyboard +/− and arrow keys, plus "Fit to screen" and "Center on me" buttons. Clamp zoom to 0.1–3.0.

```csharp
void ZoomAt(Point p, double factor)   // p = cursor position in the host's coordinates
{
    double s = Math.Clamp(_scale.ScaleX * factor, 0.1, 3.0);
    double f = s / _scale.ScaleX;
    _translate.X = p.X - f * (p.X - _translate.X);
    _translate.Y = p.Y - f * (p.Y - _translate.Y);
    _scale.ScaleX = _scale.ScaleY = s;
}
```

- Click selects and highlights the path back to the root. Double-click opens details, which is when the full photo and notes load. An `AppIcon Kind="ChevronDown"` button (with tooltip and accessible name) expands `HasMore` nodes.
- Keyboard navigation: arrow keys move to the parent, child or sibling. Set `AutomationProperties.Name` to the full name plus dates for screen readers.
- Search uses the normalized Arabic search column, then centers on the result with a brief highlight pulse.

## 10. Export and print

Export from the cached `TreeLayoutResult`, not from the screen. The same layout then renders at any size and resolution.

- **PNG:** use `RenderTargetBitmap` at 300 DPI on an off-screen visual of the full bounds. Very large trees can exhaust memory, so render tiles (e.g. 4096×4096 px) and stitch them, or export across multiple pages.
- **SVG:** write it directly from the layout (rects, paths, text). It's small, sharp and editable. Use `direction="rtl"` on Arabic text, font-family names with fallbacks, and thumbnails embedded as base64 data URIs.
- **Print:** use `PrintDialog` and let the user pick the page size; A3/A2 posters are common for the pictorial view. Offer "fit to one page" and "tile across pages" (with overlap margins and page labels). PDF output goes through the "Microsoft Print to PDF" printer; don't add a PDF library.
- Preview pictorial palettes in grayscale for black-and-white printing.

## 11. Checklist

- [ ] Union-based model; multiple partners, half-siblings and non-biological links supported.
- [ ] Identity map; pedigree collapse drawn as linked duplicates; cycle guard in every traversal.
- [ ] Lazy loading: N generations first, expand on demand, photos only when visible and zoomed in, full photos only in details.
- [ ] Layout in pure C#, on a background thread, cached by `(root, view, options, graph.Version)`.
- [ ] One `Path` per edge style; no per-node effects; zoom via `RenderTransform`.
- [ ] RTL: eldest on the start side; photos never mirrored; custom drawing mirrors coordinates, not text; pedigree subject on the right.
- [ ] Arabic: nasab display, Hijri display with `UmAlQura` range fallback, configurable deceased marker, hide-photo option, male-line view option.
- [ ] Exports render from the layout model; huge PNGs are tiled.
