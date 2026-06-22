//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// Property grid editor for a color parameter value. It paints a color swatch and opens an owner-drawn
    /// dropdown listing the parameter's enum values, each with its color preview. Bootstrap contextual names
    /// (primary, success, …), named colors (red, white, …) and hex colors (#666, #5470c6) are supported.
    /// Free-text entry in the grid cell still works.
    /// </summary>
    public class ColorParameterEditor : UITypeEditor
    {
        //Bootstrap 5 contextual colors. "default" and "" render as a plain (light) header in the views, so they
        //map to a light swatch here.
        static readonly Dictionary<string, Color> Bootstrap = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            { "primary", Color.FromArgb(0x0D, 0x6E, 0xFD) },
            { "secondary", Color.FromArgb(0x6C, 0x75, 0x7D) },
            { "success", Color.FromArgb(0x19, 0x87, 0x54) },
            { "info", Color.FromArgb(0x0D, 0xCA, 0xF0) },
            { "warning", Color.FromArgb(0xFF, 0xC1, 0x07) },
            { "danger", Color.FromArgb(0xDC, 0x35, 0x45) },
            { "light", Color.FromArgb(0xF8, 0xF9, 0xFA) },
            { "dark", Color.FromArgb(0x21, 0x25, 0x29) },
            { "default", Color.FromArgb(0xF8, 0xF9, 0xFA) }
        };

        #region Color resolution

        //Resolves a stored value to a color WITHOUT throwing (it is called on every parameter value during
        //detection, so it must not rely on exceptions for control flow). Returns false for anything that is
        //not a Bootstrap contextual name, a known color name or a hex color.
        static bool TryResolveColor(string value, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim();
            //Bootstrap contextual colors.
            if (Bootstrap.TryGetValue(v, out color)) return true;
            //Hex colors (#rgb / #rrggbb).
            if (v[0] == '#') return TryParseHex(v, out color);
            //A named color must be a single token (reject CSS snippets like "color:red;font-size:15pt;").
            if (v.IndexOfAny(new[] { ' ', ';', ':', ',', '(', '#', '.', '%' }) >= 0) return false;
            //Match against known color names without throwing; reject numeric input (which Enum.TryParse would accept).
            if (!char.IsLetter(v[0])) return false;
            if (Enum.TryParse<KnownColor>(v, true, out var known) && string.Equals(known.ToString(), v, StringComparison.OrdinalIgnoreCase))
            {
                color = Color.FromKnownColor(known);
                return true;
            }
            return false;
        }

        //Parses #rgb or #rrggbb (no exceptions).
        static bool TryParseHex(string v, out Color color)
        {
            color = Color.Empty;
            var hex = v.Substring(1);
            if (hex.Length == 3) hex = string.Format("{0}{0}{1}{1}{2}{2}", hex[0], hex[1], hex[2]);
            if (hex.Length != 6) return false;
            foreach (var c in hex) if (!Uri.IsHexDigit(c)) return false;
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            color = Color.FromArgb(r, g, b);
            return true;
        }

        //A parameter is a color parameter when it has enum values and all of its non-empty values resolve to a color.
        public static bool IsColorParameter(Parameter parameter)
        {
            if (parameter == null || parameter.Enums == null) return false;
            bool any = false;
            foreach (var value in parameter.EnumValues)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (!TryResolveColor(value, out _)) return false;
                any = true;
            }
            return any;
        }

        static void DrawSwatch(Graphics g, Rectangle bounds, Color color)
        {
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, bounds);
            }
            using (var pen = new Pen(Color.FromArgb(0x80, 0x80, 0x80)))
            {
                g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }
        }

        #endregion

        #region Painting

        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override void PaintValue(PaintValueEventArgs e)
        {
            if (TryResolveColor(e.Value as string, out var color))
            {
                var box = e.Bounds;
                box.Inflate(-1, -1);
                DrawSwatch(e.Graphics, box, color);
            }
            //else: empty / free text -> leave the swatch blank (do not crash)
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

            using (var list = new ColorListBox(editorService))
            {
                list.SetItems(GetDropDownItems(context), value as string);
                editorService.DropDownControl(list);
                return list.ChosenValue ?? value;
            }
        }

        //Lists the enum values of the edited parameter (the standard set of colors for that parameter).
        static string[] GetDropDownItems(ITypeDescriptorContext context)
        {
            var parameter = ResolveParameter(context);
            if (parameter != null && parameter.Enums != null)
            {
                var items = new List<string>();
                if (!parameter.UseOnlyEnumValues && !ContainsEmpty(parameter.EnumValues)) items.Add("");
                items.AddRange(parameter.EnumValues);
                return items.ToArray();
            }
            return new string[0];
        }

        static bool ContainsEmpty(string[] values)
        {
            foreach (var v in values) if (string.IsNullOrEmpty(v)) return true;
            return false;
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
        /// Owner-drawn ListBox rendering each value's color swatch next to its name.
        /// </summary>
        class ColorListBox : ListBox
        {
            readonly IWindowsFormsEditorService _service;
            string[] _items;
            public string ChosenValue { get; private set; }

            public ColorListBox(IWindowsFormsEditorService service)
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
                var value = _items[e.Index];
                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                var textColor = selected ? SystemColors.HighlightText : SystemColors.ControlText;

                //Color box on the left.
                var swatchBox = new Rectangle(e.Bounds.Left + 3, e.Bounds.Top + 3, e.Bounds.Height - 6, e.Bounds.Height - 6);
                if (TryResolveColor(value, out var color))
                {
                    DrawSwatch(e.Graphics, swatchBox, color);
                }

                //Value name (or "(default)" for the empty entry).
                var label = string.IsNullOrEmpty(value) ? "(default)" : value;
                var textRect = new Rectangle(swatchBox.Right + 6, e.Bounds.Top, e.Bounds.Width - swatchBox.Right - 6, e.Bounds.Height);
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
