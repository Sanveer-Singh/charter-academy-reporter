## Theme Integration and Site Theming Guide

This guide documents a repeatable approach to integrate the SB Admin 2 theme into an ASP.NET Core MVC app, establish a clean CSS hierarchy with brand tokens, and ensure accessibility and reliability. It captures decisions, rationale, and copy-pasteable snippets for future spin‑ups.

### Inputs and Decisions That Guided This Integration

- Corporate identity
  - Primary orange: `#f37021`
  - Charcoal: `#414141`
  - White: `#ffffff`
  - Grey: `#6e6e6e`
  - Yellow accent: `#f99d1c`
- Typography: Prefer Gotham; fall back to Arial → Helvetica → sans‑serif (no Google Fonts).
- Theme source: Start Bootstrap SB Admin 2 (Bootstrap 4)
- Packaging: Inline vendor assets into `wwwroot` during build (no CDN dependency)
- CSS hierarchy: Base (SB Admin 2) → Site tokens/overrides → Module CSS
- Accessibility: Focus-visible outlines, contrast helpers, skip link

---

### 1) Enable MVC and Static Files

In `Program.cs`, add MVC and serve static files from `wwwroot` (default):

```csharp
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
```

Why: SB Admin 2 ships CSS/JS/images. The app must expose those via `wwwroot` and render them through a shared layout.

---

### 2) Scaffold MVC Structure

- Create `Controllers/HomeController.cs` with `Index()` action.
- Create Razor folders: `Views/Shared/_Layout.cshtml`, `_ViewStart.cshtml`, `_ViewImports.cshtml`, and `Views/Home/Index.cshtml`.

Why: Razor layout centralizes theme includes so all pages inherit design tokens and components.

---

### 3) Copy SB Admin 2 into wwwroot at Build

Add an MSBuild target to `src/Web/Charter.Reporter.Web.csproj` to copy assets from the repo’s `startbootstrap-sb-admin-2-gh-pages` into `wwwroot`:

```xml
<ItemGroup>
  <SbVendor Include="$(MSBuildProjectDirectory)\..\..\startbootstrap-sb-admin-2-gh-pages\vendor\**\*.*" />
  <SbCss Include="$(MSBuildProjectDirectory)\..\..\startbootstrap-sb-admin-2-gh-pages\css\**\*.*" />
  <SbJs Include="$(MSBuildProjectDirectory)\..\..\startbootstrap-sb-admin-2-gh-pages\js\**\*.*" />
  <SbImg Include="$(MSBuildProjectDirectory)\..\..\startbootstrap-sb-admin-2-gh-pages\img\**\*.*" />
</ItemGroup>

<Target Name="CopySbAdminAssets" AfterTargets="Build">
  <PropertyGroup>
    <WebRoot>$(MSBuildProjectDirectory)\wwwroot</WebRoot>
  </PropertyGroup>
  <MakeDir Directories="$(WebRoot)\vendor" />
  <MakeDir Directories="$(WebRoot)\css" />
  <MakeDir Directories="$(WebRoot)\js" />
  <MakeDir Directories="$(WebRoot)\img" />
  <Copy SourceFiles="@(SbVendor)" DestinationFolder="$(WebRoot)\vendor\%(RecursiveDir)" SkipUnchangedFiles="true" />
  <Copy SourceFiles="@(SbCss)" DestinationFolder="$(WebRoot)\css\%(RecursiveDir)" SkipUnchangedFiles="true" />
  <Copy SourceFiles="@(SbJs)" DestinationFolder="$(WebRoot)\js\%(RecursiveDir)" SkipUnchangedFiles="true" />
  <Copy SourceFiles="@(SbImg)" DestinationFolder="$(WebRoot)\img\%(RecursiveDir)" SkipUnchangedFiles="true" />
</Target>
```

Why: Keeps deployments self-contained, removes CDN/runtime path coupling, and ensures correct versions across environments.

---

### 4) Shared Layout Wiring

`Views/Shared/_Layout.cshtml` includes local assets and corporate typography:

```html
<link rel="stylesheet" href="~/vendor/fontawesome-free/css/all.min.css" />
<link rel="stylesheet" href="~/css/sb-admin-2.min.css" />
<link rel="stylesheet" href="~/css/variables.css" />
<link rel="stylesheet" href="~/css/site.css" />
<style>body{font-family:Gotham,Arial,Helvetica,sans-serif;}</style>

<script src="~/vendor/jquery/jquery.min.js"></script>
<script src="~/vendor/bootstrap/js/bootstrap.bundle.min.js"></script>
<script src="~/vendor/jquery-easing/jquery.easing.min.js"></script>
<script src="~/js/sb-admin-2.min.js"></script>
```

Why: The order preserves the cascade—base theme first, then tokens, then site overrides.

---

### 5) CSS Hierarchy and Design Tokens

Create two site-level stylesheets in `wwwroot/css/`:

1) `variables.css` (brand tokens)

```css
:root {
  --brand-orange:#f37021; --brand-charcoal:#414141; --brand-white:#fff;
  --brand-grey:#6e6e6e; --brand-yellow:#f99d1c;
  --charter-primary:var(--brand-orange);
  --charter-secondary:var(--brand-charcoal);
  --charter-accent:var(--brand-yellow);
  --charter-bg:var(--brand-white);
  --charter-text:var(--brand-grey);
  --spacing-xs:.25rem; --spacing-sm:.5rem; --spacing-md:1rem; --spacing-lg:1.5rem; --spacing-xl:2rem;
  --font-size-sm:.875rem; --font-size-base:1rem; --font-size-lg:1.125rem; --font-size-xl:1.25rem;
  --shadow-sm:0 1px 2px 0 rgba(0,0,0,.05); --shadow-md:0 4px 6px -1px rgba(0,0,0,.1); --shadow-lg:0 10px 15px -3px rgba(0,0,0,.1);
  --transition-fast:150ms ease-in-out; --transition-base:250ms ease-in-out; --transition-slow:350ms ease-in-out;
}
@media (prefers-color-scheme: dark){:root{--charter-primary:#ff8a3d;--charter-text:#e5e7eb;}}
```

2) `site.css` (site overrides + accessibility)

```css
body{background:var(--charter-bg);color:var(--charter-text);} 
.bg-gradient-primary{background-image:linear-gradient(180deg,var(--charter-primary) 10%,var(--charter-secondary) 100%)!important}
.btn-primary{background:var(--charter-primary);border-color:var(--charter-primary);transition:all var(--transition-fast)}
.btn-primary:hover{filter:brightness(.9);box-shadow:var(--shadow-md)}
.card{box-shadow:var(--shadow-sm);transition:box-shadow var(--transition-base)}.card:hover{box-shadow:var(--shadow-md)}
.skip-link{position:absolute;top:-40px;left:0;background:var(--charter-primary);color:#fff;padding:var(--spacing-sm) var(--spacing-md);z-index:100;text-decoration:none}
.skip-link:focus{top:0}
*:focus-visible{outline:3px solid var(--charter-accent);outline-offset:2px}
.high-contrast{color:#000!important;background:#fff!important}.low-contrast{color:#111!important;background:#f8f9fc!important}
```

Why: Tokens enable brand consistency and future theming; overrides adapt SB Admin 2 to corporate look while preserving utility classes.

Module CSS: add per-feature files under `wwwroot/css/modules/` and include via Razor `@section Styles { }` when needed.

---

### 6) Accessibility Standards

- Focus visibility: global `:focus-visible` outline using the accent color
- Skip link: quickly jump to main content via keyboard
- Contrast helpers: utility classes to evaluate/adjust contrast in UI experiments

Why: Meets key WCAG 2.1 usability expectations with minimal overhead.

---

### 7) Layout Shell and Content Area

The layout implements the SB Admin 2 sidebar/topbar shell. Pages render inside `#main-content` so Skip Link targets are stable. Keep semantic headings and ARIA labels when extending navigation and dashboards.

---

### 8) Build & Verify

1. `dotnet build` — triggers asset copy target and compiles Razor views.
2. Run Web project and open `/` — verify:
   - Theme CSS/JS served from `~/css`, `~/js`, `~/vendor`
   - Brand colors applied to gradient sidebar, buttons, cards
   - Focus outlines visible; Skip Link appears on tab
   - No 404s for static assets

---

### 9) Production Considerations

- Pin vendor versions by committing SB Admin 2 sources into the repo (already done) and copying via MSBuild.
- Optionally fingerprint assets (hash in filename) using a bundler if aggressive caching is required.
- If Gotham is required, add licensed webfont files under `wwwroot/fonts` with `@font-face` and preload tags.
- CSP: if you later add CDNs, configure Content Security Policy headers accordingly.

---

### 10) Reuse Checklist (Copy/Paste)

- [ ] Add MVC + Static Files in `Program.cs`
- [ ] Create Razor structure (`_Layout`, `_ViewStart`, `_ViewImports`, `Home/Index`)
- [ ] Add MSBuild copy target (vendor/css/js/img → `wwwroot`)
- [ ] Wire layout to local `~/vendor`, `~/css`, `~/js`
- [ ] Create `wwwroot/css/variables.css` with brand tokens
- [ ] Create `wwwroot/css/site.css` for overrides and accessibility
- [ ] Remove any CDN font; set `font-family` to corporate stack
- [ ] Add Skip Link and `:focus-visible` outline
- [ ] Build and validate asset paths and styles
- [ ] Add per-module CSS under `wwwroot/css/modules/` as features ship

---

### Rationale Summary

- Local asset copy makes builds deterministic and offline-robust.
- Tokenized CSS separates changeable brand inputs from stable theme base.
- Layout-first integration ensures site-wide availability of theme utilities and tokens.
- Minimal, measurable accessibility enhancements improve usability without redesigning components.


