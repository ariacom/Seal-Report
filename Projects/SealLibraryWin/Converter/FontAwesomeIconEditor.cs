//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// Property grid editor for a Font Awesome icon class. It paints the selected glyph in the swatch
    /// box and opens an owner-drawn dropdown listing a curated sample set with a live glyph preview.
    /// Free-text entry in the grid cell is still honored for any value outside the sample list.
    /// </summary>
    public class FontAwesomeIconEditor : UITypeEditor
    {
        /// <summary>
        /// A curated sample icon. CssClass is the value stored on the property; Variant selects the
        /// font file; CodePoint is the glyph index in that font (verified against the repository
        /// fontawesome all.min.css).
        /// </summary>
        class IconSample
        {
            public string CssClass;
            public string Variant; //"solid", "regular" or "brands"
            public int CodePoint;
            public IconSample(string cssClass, string variant, int codePoint)
            {
                CssClass = cssClass; Variant = variant; CodePoint = codePoint;
            }
        }

        //Same sample list as the legacy FontAwesomeIconConverter, with the glyph code point of each class.
        //Code points verified against Repository/Views/lib/fontawesome/css/all.min.css (FA6 Free).
        static readonly IconSample[] Samples = new IconSample[] {
            new IconSample("", "solid", 0),
            new IconSample("fa-solid fa-folder", "solid", 0xF07B),
            new IconSample("fa-regular fa-folder", "regular", 0xF07B),
            new IconSample("fa-solid fa-folder-open", "solid", 0xF07C),
            new IconSample("fa-solid fa-building", "solid", 0xF1AD),
            new IconSample("fa-solid fa-house", "solid", 0xF015),
            new IconSample("fa-solid fa-briefcase", "solid", 0xF0B1),
            new IconSample("fa-solid fa-users", "solid", 0xF0C0),
            new IconSample("fa-solid fa-user", "solid", 0xF007),
            new IconSample("fa-solid fa-globe", "solid", 0xF0AC),
            new IconSample("fa-solid fa-database", "solid", 0xF1C0),
            new IconSample("fa-solid fa-table", "solid", 0xF0CE),
            new IconSample("fa-solid fa-chart-bar", "solid", 0xF080),
            new IconSample("fa-solid fa-chart-line", "solid", 0xF201),
            new IconSample("fa-solid fa-chart-pie", "solid", 0xF200),
            new IconSample("fa-solid fa-gauge-high", "solid", 0xF625),
            new IconSample("fa-solid fa-file-lines", "solid", 0xF15C),
            new IconSample("fa-solid fa-clipboard-list", "solid", 0xF46D),
            new IconSample("fa-solid fa-box-archive", "solid", 0xF187),
            new IconSample("fa-solid fa-gear", "solid", 0xF013),
            new IconSample("fa-solid fa-star", "solid", 0xF005),
            new IconSample("fa-solid fa-flag", "solid", 0xF024),
            new IconSample("fa-solid fa-money-bill-trend-up", "solid", 0xE529),
            //Files & documents
            new IconSample("fa-solid fa-folder-tree", "solid", 0xF802),
            new IconSample("fa-solid fa-file", "solid", 0xF15B),
            new IconSample("fa-solid fa-file-pdf", "solid", 0xF1C1),
            new IconSample("fa-solid fa-file-excel", "solid", 0xF1C3),
            new IconSample("fa-solid fa-file-csv", "solid", 0xF6DD),
            new IconSample("fa-solid fa-file-code", "solid", 0xF1C9),
            new IconSample("fa-solid fa-book", "solid", 0xF02D),
            //Lists & organization
            new IconSample("fa-solid fa-list", "solid", 0xF03A),
            new IconSample("fa-solid fa-list-check", "solid", 0xF0AE),
            new IconSample("fa-solid fa-bookmark", "solid", 0xF02E),
            new IconSample("fa-solid fa-tag", "solid", 0xF02B),
            new IconSample("fa-solid fa-tags", "solid", 0xF02C),
            new IconSample("fa-solid fa-filter", "solid", 0xF0B0),
            new IconSample("fa-solid fa-sitemap", "solid", 0xF0E8),
            new IconSample("fa-solid fa-diagram-project", "solid", 0xF542),
            new IconSample("fa-solid fa-layer-group", "solid", 0xF5FD),
            //Time & communication
            new IconSample("fa-solid fa-calendar-days", "solid", 0xF073),
            new IconSample("fa-solid fa-clock", "solid", 0xF017),
            new IconSample("fa-solid fa-bell", "solid", 0xF0F3),
            new IconSample("fa-solid fa-envelope", "solid", 0xF0E0),
            //Security
            new IconSample("fa-solid fa-lock", "solid", 0xF023),
            new IconSample("fa-solid fa-key", "solid", 0xF084),
            new IconSample("fa-solid fa-shield-halved", "solid", 0xF3ED),
            //Infrastructure
            new IconSample("fa-solid fa-cloud", "solid", 0xF0C2),
            new IconSample("fa-solid fa-server", "solid", 0xF233),
            new IconSample("fa-solid fa-network-wired", "solid", 0xF6FF),
            new IconSample("fa-solid fa-microchip", "solid", 0xF2DB),
            new IconSample("fa-solid fa-cube", "solid", 0xF1B2),
            new IconSample("fa-solid fa-cubes", "solid", 0xF1B3),
            new IconSample("fa-solid fa-boxes-stacked", "solid", 0xF468),
            new IconSample("fa-solid fa-warehouse", "solid", 0xF494),
            new IconSample("fa-solid fa-industry", "solid", 0xF275),
            //Finance & commerce
            new IconSample("fa-solid fa-cart-shopping", "solid", 0xF07A),
            new IconSample("fa-solid fa-dollar-sign", "solid", 0x24),
            new IconSample("fa-solid fa-coins", "solid", 0xF51E),
            new IconSample("fa-solid fa-credit-card", "solid", 0xF09D),
            new IconSample("fa-solid fa-receipt", "solid", 0xF543),
            new IconSample("fa-solid fa-sack-dollar", "solid", 0xF81D),
            //Charts (additional)
            new IconSample("fa-solid fa-chart-area", "solid", 0xF1FE),
            new IconSample("fa-solid fa-chart-column", "solid", 0xE0E3),
            //Tools & development
            new IconSample("fa-solid fa-wrench", "solid", 0xF0AD),
            new IconSample("fa-solid fa-screwdriver-wrench", "solid", 0xF7D9),
            new IconSample("fa-solid fa-toolbox", "solid", 0xF552),
            new IconSample("fa-solid fa-bug", "solid", 0xF188),
            new IconSample("fa-solid fa-code", "solid", 0xF121),
            new IconSample("fa-solid fa-terminal", "solid", 0xF120),
            //Misc & categories
            new IconSample("fa-solid fa-circle-info", "solid", 0xF05A),
            new IconSample("fa-solid fa-heart", "solid", 0xF004),
            new IconSample("fa-solid fa-trophy", "solid", 0xF091),
            new IconSample("fa-solid fa-crown", "solid", 0xF521),
            new IconSample("fa-solid fa-gift", "solid", 0xF06B),
            new IconSample("fa-solid fa-rocket", "solid", 0xF135),
            new IconSample("fa-solid fa-lightbulb", "solid", 0xF0EB),
            new IconSample("fa-solid fa-flask", "solid", 0xF0C3),
            new IconSample("fa-solid fa-graduation-cap", "solid", 0xF19D),
            new IconSample("fa-solid fa-hospital", "solid", 0xF0F8),
            new IconSample("fa-solid fa-truck", "solid", 0xF0D1),
            new IconSample("fa-solid fa-map", "solid", 0xF279),
            new IconSample("fa-solid fa-location-dot", "solid", 0xF3C5),
            new IconSample("fa-solid fa-magnifying-glass", "solid", 0xF002),
            new IconSample("fa-solid fa-compass", "solid", 0xF14E),
            new IconSample("fa-solid fa-puzzle-piece", "solid", 0xF12E),
            new IconSample("fa-solid fa-bolt", "solid", 0xF0E7),
            new IconSample("fa-solid fa-fire", "solid", 0xF06D),
            new IconSample("fa-solid fa-leaf", "solid", 0xF06C),
            new IconSample("fa-solid fa-tree", "solid", 0xF1BB)
        };

        #region Font loading

        //Private collections must be kept alive for the loaded fonts to keep rendering.
        static PrivateFontCollection _solidCollection, _regularCollection, _brandsCollection;
        static FontFamily _solidFamily, _regularFamily, _brandsFamily;
        static bool _loadAttempted = false;
        static readonly object _lock = new object();

        static FontFamily LoadFamily(string fileName, ref PrivateFontCollection collection)
        {
            try
            {
                var baseFolder = RepositoryServer.ViewsFolder;
                if (string.IsNullOrEmpty(baseFolder)) return null;
                var path = Path.Combine(baseFolder, "lib", "fontawesome", "webfonts", fileName);
                if (!File.Exists(path)) return null;
                collection = new PrivateFontCollection();
                collection.AddFontFile(path);
                return collection.Families.Length > 0 ? collection.Families[0] : null;
            }
            catch
            {
                return null;
            }
        }

        static void EnsureFontsLoaded()
        {
            if (_loadAttempted) return;
            lock (_lock)
            {
                if (_loadAttempted) return;
                _solidFamily = LoadFamily("fa-solid-900.ttf", ref _solidCollection);
                _regularFamily = LoadFamily("fa-regular-400.ttf", ref _regularCollection);
                _brandsFamily = LoadFamily("fa-brands-400.ttf", ref _brandsCollection);
                _loadAttempted = true;
            }
        }

        static FontFamily GetFamily(string variant)
        {
            EnsureFontsLoaded();
            switch (variant)
            {
                case "regular": return _regularFamily;
                case "brands": return _brandsFamily;
                default: return _solidFamily;
            }
        }

        //GDI+ throws when building a Font with a style the family does not provide (the FA solid family
        //typically exposes only Regular, but be defensive and pick whatever is available).
        static Font CreateFont(FontFamily family, float emSize)
        {
            if (family == null) return null;
            foreach (var style in new[] { FontStyle.Regular, FontStyle.Bold, FontStyle.Italic, FontStyle.Bold | FontStyle.Italic })
            {
                if (family.IsStyleAvailable(style))
                {
                    try { return new Font(family, emSize, style, GraphicsUnit.Pixel); }
                    catch { }
                }
            }
            return null;
        }

        #endregion

        #region Glyph resolution / painting

        //Full Font Awesome class -> code point map, parsed once from the repository all.min.css so that ANY
        //icon (not just the curated samples) can be previewed. Keyed by icon name without the "fa-" prefix.
        static Dictionary<string, int> _codePoints;
        static readonly object _cpLock = new object();

        static Dictionary<string, int> CodePoints
        {
            get
            {
                if (_codePoints != null) return _codePoints;
                lock (_cpLock)
                {
                    if (_codePoints != null) return _codePoints;
                    var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    try
                    {
                        var baseFolder = RepositoryServer.ViewsFolder;
                        if (!string.IsNullOrEmpty(baseFolder))
                        {
                            var css = Path.Combine(baseFolder, "lib", "fontawesome", "css", "all.min.css");
                            if (File.Exists(css))
                            {
                                var text = File.ReadAllText(css);
                                //Each rule block ends with '}'. A glyph block carries --fa:"\fXXXX" and one or
                                //more .fa-name selectors (grouped aliases share the same code point).
                                foreach (var block in text.Split('}'))
                                {
                                    var cp = Regex.Match(block, "--fa:\"\\\\([0-9a-fA-F]+)\"");
                                    if (!cp.Success) continue;
                                    int code = Convert.ToInt32(cp.Groups[1].Value, 16);
                                    foreach (Match sel in Regex.Matches(block, "\\.fa-([a-z0-9-]+)"))
                                    {
                                        map[sel.Groups[1].Value] = code;
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                    //Fall back to the curated samples for any name the css did not provide.
                    foreach (var s in Samples)
                    {
                        if (s.CodePoint == 0) continue;
                        var name = IconName(s.CssClass);
                        if (name != null && !map.ContainsKey(name)) map[name] = s.CodePoint;
                    }
                    _codePoints = map;
                    return _codePoints;
                }
            }
        }

        //Style tokens that are not the icon name itself.
        static readonly HashSet<string> StyleTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "fa", "fa-solid", "fas", "fa-regular", "far", "fa-brands", "fab",
            "fa-light", "fal", "fa-thin", "fat", "fa-duotone", "fad", "fa-sharp"
        };

        //Extracts the icon name (without "fa-") from a class string, e.g. "fa-solid fa-folder-open" -> "folder-open".
        static string IconName(string cssClass)
        {
            if (string.IsNullOrWhiteSpace(cssClass)) return null;
            foreach (var token in cssClass.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (StyleTokens.Contains(token)) continue;
                if (token.StartsWith("fa-", StringComparison.OrdinalIgnoreCase)) return token.Substring(3);
            }
            return null;
        }

        //Resolves the font variant (solid / regular / brands) from the class tokens.
        static string VariantOf(string cssClass)
        {
            if (!string.IsNullOrEmpty(cssClass))
            {
                if (Regex.IsMatch(cssClass, @"(^|\s)(fa-regular|far)(\s|$)", RegexOptions.IgnoreCase)) return "regular";
                if (Regex.IsMatch(cssClass, @"(^|\s)(fa-brands|fab)(\s|$)", RegexOptions.IgnoreCase)) return "brands";
            }
            return "solid";
        }

        //Resolves any stored icon value (Font Awesome class or legacy Glyphicon) to (variant, codepoint).
        //Returns false when nothing can be resolved so callers can simply skip the glyph (no crash).
        static bool TryResolve(string storedIcon, out string variant, out int codePoint)
        {
            variant = "solid"; codePoint = 0;
            if (string.IsNullOrWhiteSpace(storedIcon)) return false;
            //Normalize the value the same way the web side does (legacy Glyphicon names -> Font Awesome class),
            //so the preview matches what is actually rendered.
            var cssClass = Parameter.GetFontAwesomeIcon(storedIcon.Trim());
            var name = IconName(cssClass);
            if (name == null) return false;
            if (!CodePoints.TryGetValue(name, out codePoint)) return false;
            variant = VariantOf(cssClass);
            return true;
        }

        static void DrawGlyph(Graphics g, Rectangle bounds, string variant, int codePoint, Color color)
        {
            var family = GetFamily(variant);
            //Size the glyph to the available box height (the property grid swatch is ~16px tall).
            float emSize = Math.Max(8f, Math.Min(bounds.Width, bounds.Height) - 1f);
            using (var font = CreateFont(family, emSize))
            {
                if (font == null) return;
                var oldHint = g.TextRenderingHint;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using (var brush = new SolidBrush(color))
                using (var sf = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(char.ConvertFromUtf32(codePoint), font, brush, bounds, sf);
                }
                g.TextRenderingHint = oldHint;
            }
        }

        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override void PaintValue(PaintValueEventArgs e)
        {
            if (TryResolve(e.Value as string, out var variant, out var codePoint))
            {
                DrawGlyph(e.Graphics, e.Bounds, variant, codePoint, Color.Black);
            }
            //else: free text or empty -> leave the swatch blank (do not crash)
        }

        #endregion

        #region Dropdown editing

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        public override bool IsDropDownResizable
        {
            get { return true; }
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var editorService = provider?.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            if (editorService == null) return value;

            using (var list = new IconListBox(editorService))
            {
                list.SetItems(GetDropDownItems(context), value as string);
                editorService.DropDownControl(list);
                return list.ChosenValue ?? value;
            }
        }

        //The dropdown lists the enum values of the edited parameter when available (e.g. the widget icon
        //parameter exposes the full Font Awesome list); otherwise it shows the curated sample classes.
        static string[] GetDropDownItems(ITypeDescriptorContext context)
        {
            var parameter = ResolveParameter(context);
            if (parameter != null && parameter.Enums != null)
            {
                var items = new List<string>();
                if (!parameter.UseOnlyEnumValues) items.Add("");
                items.AddRange(parameter.EnumValues);
                return items.ToArray();
            }
            var samples = new List<string>();
            foreach (var s in Samples) samples.Add(s.CssClass);
            return samples.ToArray();
        }

        //The edited object is a Parameter when shown directly, or a ParametersEditor when view parameters are
        //flattened into the grid (the property name e.g. "e3" maps back to its Parameter).
        static Parameter ResolveParameter(ITypeDescriptorContext context)
        {
            if (context == null) return null;
            if (context.Instance is Parameter parameter) return parameter;
            if (context.Instance is ParametersEditor editor && context.PropertyDescriptor != null)
            {
                return editor.GetParameter(context.PropertyDescriptor.Name);
            }
            return null;
        }

        /// <summary>
        /// Owner-drawn ListBox rendering each icon class's glyph next to its class name.
        /// </summary>
        class IconListBox : ListBox
        {
            readonly IWindowsFormsEditorService _service;
            string[] _items;
            public string ChosenValue { get; private set; }

            public IconListBox(IWindowsFormsEditorService service)
            {
                _service = service;
                BorderStyle = BorderStyle.None;
                DrawMode = DrawMode.OwnerDrawFixed;
                ItemHeight = 22;
                IntegralHeight = false;
            }

            public void SetItems(string[] items, string current)
            {
                _items = items ?? new string[0];
                Items.Clear();
                int selected = -1;
                for (int i = 0; i < _items.Length; i++)
                {
                    Items.Add(_items[i]);
                    if (string.Equals(_items[i], (current ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) selected = i;
                }
                SelectedIndex = selected;
                //Height for up to ~12 rows then scroll.
                Height = ItemHeight * Math.Min(Math.Max(_items.Length, 1), 12) + 2;
            }

            protected override void OnDrawItem(DrawItemEventArgs e)
            {
                e.DrawBackground();
                if (e.Index < 0 || _items == null || e.Index >= _items.Length) return;
                var cssClass = _items[e.Index];
                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                var textColor = selected ? SystemColors.HighlightText : SystemColors.ControlText;

                //Glyph box on the left.
                var glyphBox = new Rectangle(e.Bounds.Left + 2, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4);
                if (TryResolve(cssClass, out var variant, out var codePoint))
                {
                    DrawGlyph(e.Graphics, glyphBox, variant, codePoint, textColor);
                }

                //Class name (or "(default)" for the empty entry).
                var label = string.IsNullOrEmpty(cssClass) ? "(default)" : cssClass;
                var textRect = new Rectangle(glyphBox.Right + 6, e.Bounds.Top, e.Bounds.Width - glyphBox.Right - 6, e.Bounds.Height);
                using (var brush = new SolidBrush(textColor))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                {
                    e.Graphics.DrawString(label, e.Font, brush, textRect, sf);
                }
                e.DrawFocusRectangle();
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                Commit();
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (e.KeyCode == Keys.Enter) Commit();
                else if (e.KeyCode == Keys.Escape) _service.CloseDropDown();
            }

            void Commit()
            {
                if (SelectedIndex >= 0 && _items != null) ChosenValue = _items[SelectedIndex];
                _service.CloseDropDown();
            }
        }

        #endregion
    }
}
