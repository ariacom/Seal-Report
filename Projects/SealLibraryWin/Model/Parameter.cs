//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System;
using Seal.Helpers;
using MySqlX.XDevAPI.Common;
using System.Globalization;

#if WINDOWS
using Seal.Forms;
using System.Drawing.Design;
#endif


namespace Seal.Model
{
    /// <summary>
    /// Parameters are used to configure report templates, outputs and repository security
    /// </summary>
    public class Parameter : RootComponent
    {
        public const string ReportFormatParameter = "report_format";
        public const string DrillEnabledParameter = "drill_enabled";
        public const string DrillAllParameter = "drill_all";
        public const string SubReportsEnabledParameter = "subreports_enabled";
        public const string ServerPaginationParameter = "serverpagination_enabled";
        public const string EnableResultsMenuParameter = "resultsmenu_enabled";
        public const string ForceExecutionParameter = "force_execution";
        public const string ForceRefreshParameter = "force_refresh";
        public const string ForceModelsLoad = "force_models_load";
        public const string NVD3AddNullPointParameter = "nvd3_add_null_point";
        public const string ColumnsHiddenParameter = "columns_hidden";
        public const string CSVUtf8Parameter = "csv_utf8";
        public const string AutoScrollParameter = "messages_autoscroll";
        public const string RestrictionsExecView = "restrictions_exec_view";
        public const string NavigationView = "navigation_view";
        public const string ResultOptionNameTabs = "|tabs"; //Generic requires |

        /// <summary>
        /// Legacy Bootstrap 3 Glyphicon names still allowed as stored values (e.g. widget_icon).
        /// </summary>
        public static string[] Glyphicons = new string[] { "asterisk", "plus", "minus", "eur", "euro", "cloud", "envelope", "pencil", "glass", "music", "search", "heart", "star", "star-empty", "user", "film", "th-large", "th", "th-list", "ok", "remove", "zoom-in", "zoom-out", "off", "signal", "cog", "trash", "home", "file", "time", "road", "download-alt", "download", "upload", "inbox", "play-circle", "repeat", "refresh", "list-alt", "lock", "flag", "headphones", "volume-off", "volume-down", "volume-up", "qrcode", "barcode", "tag", "tags", "book", "bookmark", "print", "camera", "font", "bold", "italic", "text-height", "text-width", "align-left", "align-center", "align-right", "align-justify", "list", "indent-left", "indent-right", "facetime-video", "picture", "map-marker", "adjust", "tint", "edit", "share", "check", "move", "step-backward", "fast-backward", "backward", "play", "pause", "stop", "forward", "fast-forward", "step-forward", "eject", "chevron-left", "chevron-right", "plus-sign", "minus-sign", "remove-sign", "ok-sign", "question-sign", "info-sign", "screenshot", "remove-circle", "ok-circle", "ban-circle", "arrow-left", "arrow-right", "arrow-up", "arrow-down", "share-alt", "resize-full", "resize-small", "exclamation-sign", "gift", "leaf", "fire", "eye-open", "eye-close", "warning-sign", "plane", "calendar", "random", "comment", "magnet", "chevron-up", "chevron-down", "retweet", "shopping-cart", "folder-close", "folder-open", "resize-vertical", "resize-horizontal", "hdd", "bullhorn", "bell", "certificate", "thumbs-up", "thumbs-down", "hand-right", "hand-left", "hand-up", "hand-down", "circle-arrow-right", "circle-arrow-left", "circle-arrow-up", "circle-arrow-down", "globe", "wrench", "tasks", "filter", "briefcase", "fullscreen", "dashboard", "paperclip", "heart-empty", "link", "phone", "pushpin", "usd", "gbp", "sort", "sort-by-alphabet", "sort-by-alphabet-alt", "sort-by-order", "sort-by-order-alt", "sort-by-attributes", "sort-by-attributes-alt", "unchecked", "expand", "collapse-down", "collapse-up", "log-in", "flash", "log-out", "new-window", "record", "save", "open", "saved", "import", "export", "send", "floppy-disk", "floppy-saved", "floppy-remove", "floppy-save", "floppy-open", "credit-card", "transfer", "cutlery", "header", "compressed", "earphone", "phone-alt", "tower", "stats", "sd-video", "hd-video", "subtitles", "sound-stereo", "sound-dolby", "sound-5-1", "sound-6-1", "sound-7-1", "copyright-mark", "registration-mark", "cloud-download", "cloud-upload", "tree-conifer", "tree-deciduous", "cd", "save-file", "open-file", "level-up", "copy", "paste", "alert", "equalizer", "king", "queen", "pawn", "bishop", "knight", "baby-formula", "tent", "blackboard", "bed", "apple", "erase", "hourglass", "lamp", "duplicate", "piggy-bank", "scissors", "bitcoin", "yen", "ruble", "scale", "ice-lolly", "ice-lolly-tasted", "education", "option-horizontal", "option-vertical", "menu-hamburger", "modal-window", "oil", "grain", "sunglasses", "text-size", "text-color", "text-background", "object-align-top", "object-align-bottom", "object-align-horizontal", "object-align-left", "object-align-vertical", "object-align-right", "triangle-right", "triangle-left", "triangle-bottom", "triangle-top", "console", "superscript", "subscript", "menu-left", "menu-right", "menu-down", "menu-up" };

        /// <summary>
        /// Maps each legacy Bootstrap 3 Glyphicon name to its Font Awesome 6 (Free) equivalent.
        /// Used to migrate stored icon values (e.g. widget_icon) after the Bootstrap 5 / FA6 migration,
        /// which dropped the Glyphicons font.
        /// </summary>
        public static Dictionary<string, string> GlyphiconToFontAwesome = new Dictionary<string, string>() {
            { "asterisk", "fa-solid fa-asterisk" }, { "plus", "fa-solid fa-plus" }, { "minus", "fa-solid fa-minus" }, { "eur", "fa-solid fa-euro-sign" }, { "euro", "fa-solid fa-euro-sign" },
            { "cloud", "fa-solid fa-cloud" }, { "envelope", "fa-solid fa-envelope" }, { "pencil", "fa-solid fa-pencil" }, { "glass", "fa-solid fa-martini-glass" }, { "music", "fa-solid fa-music" },
            { "search", "fa-solid fa-magnifying-glass" }, { "heart", "fa-solid fa-heart" }, { "star", "fa-solid fa-star" }, { "star-empty", "fa-regular fa-star" }, { "user", "fa-solid fa-user" },
            { "film", "fa-solid fa-film" }, { "th-large", "fa-solid fa-table-cells-large" }, { "th", "fa-solid fa-table-cells" }, { "th-list", "fa-solid fa-table-list" }, { "ok", "fa-solid fa-check" },
            { "remove", "fa-solid fa-xmark" }, { "zoom-in", "fa-solid fa-magnifying-glass-plus" }, { "zoom-out", "fa-solid fa-magnifying-glass-minus" }, { "off", "fa-solid fa-power-off" }, { "signal", "fa-solid fa-signal" },
            { "cog", "fa-solid fa-gear" }, { "trash", "fa-solid fa-trash" }, { "home", "fa-solid fa-house" }, { "file", "fa-solid fa-file" }, { "time", "fa-regular fa-clock" },
            { "road", "fa-solid fa-road" }, { "download-alt", "fa-solid fa-download" }, { "download", "fa-solid fa-circle-down" }, { "upload", "fa-solid fa-circle-up" }, { "inbox", "fa-solid fa-inbox" },
            { "play-circle", "fa-solid fa-circle-play" }, { "repeat", "fa-solid fa-repeat" }, { "refresh", "fa-solid fa-arrows-rotate" }, { "list-alt", "fa-solid fa-rectangle-list" }, { "lock", "fa-solid fa-lock" },
            { "flag", "fa-solid fa-flag" }, { "headphones", "fa-solid fa-headphones" }, { "volume-off", "fa-solid fa-volume-xmark" }, { "volume-down", "fa-solid fa-volume-low" }, { "volume-up", "fa-solid fa-volume-high" },
            { "qrcode", "fa-solid fa-qrcode" }, { "barcode", "fa-solid fa-barcode" }, { "tag", "fa-solid fa-tag" }, { "tags", "fa-solid fa-tags" }, { "book", "fa-solid fa-book" },
            { "bookmark", "fa-solid fa-bookmark" }, { "print", "fa-solid fa-print" }, { "camera", "fa-solid fa-camera" }, { "font", "fa-solid fa-font" }, { "bold", "fa-solid fa-bold" },
            { "italic", "fa-solid fa-italic" }, { "text-height", "fa-solid fa-text-height" }, { "text-width", "fa-solid fa-text-width" }, { "align-left", "fa-solid fa-align-left" }, { "align-center", "fa-solid fa-align-center" },
            { "align-right", "fa-solid fa-align-right" }, { "align-justify", "fa-solid fa-align-justify" }, { "list", "fa-solid fa-list" }, { "indent-left", "fa-solid fa-outdent" }, { "indent-right", "fa-solid fa-indent" },
            { "facetime-video", "fa-solid fa-video" }, { "picture", "fa-regular fa-image" }, { "map-marker", "fa-solid fa-location-dot" }, { "adjust", "fa-solid fa-circle-half-stroke" }, { "tint", "fa-solid fa-droplet" },
            { "edit", "fa-solid fa-pen-to-square" }, { "share", "fa-solid fa-share" }, { "check", "fa-regular fa-square-check" }, { "move", "fa-solid fa-up-down-left-right" }, { "step-backward", "fa-solid fa-backward-step" },
            { "fast-backward", "fa-solid fa-backward-fast" }, { "backward", "fa-solid fa-backward" }, { "play", "fa-solid fa-play" }, { "pause", "fa-solid fa-pause" }, { "stop", "fa-solid fa-stop" },
            { "forward", "fa-solid fa-forward" }, { "fast-forward", "fa-solid fa-forward-fast" }, { "step-forward", "fa-solid fa-forward-step" }, { "eject", "fa-solid fa-eject" }, { "chevron-left", "fa-solid fa-chevron-left" },
            { "chevron-right", "fa-solid fa-chevron-right" }, { "plus-sign", "fa-solid fa-circle-plus" }, { "minus-sign", "fa-solid fa-circle-minus" }, { "remove-sign", "fa-solid fa-circle-xmark" }, { "ok-sign", "fa-solid fa-circle-check" },
            { "question-sign", "fa-solid fa-circle-question" }, { "info-sign", "fa-solid fa-circle-info" }, { "screenshot", "fa-solid fa-crosshairs" }, { "remove-circle", "fa-solid fa-circle-xmark" }, { "ok-circle", "fa-regular fa-circle-check" },
            { "ban-circle", "fa-solid fa-ban" }, { "arrow-left", "fa-solid fa-arrow-left" }, { "arrow-right", "fa-solid fa-arrow-right" }, { "arrow-up", "fa-solid fa-arrow-up" }, { "arrow-down", "fa-solid fa-arrow-down" },
            { "share-alt", "fa-solid fa-share-nodes" }, { "resize-full", "fa-solid fa-expand" }, { "resize-small", "fa-solid fa-compress" }, { "exclamation-sign", "fa-solid fa-circle-exclamation" }, { "gift", "fa-solid fa-gift" },
            { "leaf", "fa-solid fa-leaf" }, { "fire", "fa-solid fa-fire" }, { "eye-open", "fa-solid fa-eye" }, { "eye-close", "fa-solid fa-eye-slash" }, { "warning-sign", "fa-solid fa-triangle-exclamation" },
            { "plane", "fa-solid fa-plane" }, { "calendar", "fa-regular fa-calendar" }, { "random", "fa-solid fa-shuffle" }, { "comment", "fa-solid fa-comment" }, { "magnet", "fa-solid fa-magnet" },
            { "chevron-up", "fa-solid fa-chevron-up" }, { "chevron-down", "fa-solid fa-chevron-down" }, { "retweet", "fa-solid fa-retweet" }, { "shopping-cart", "fa-solid fa-cart-shopping" }, { "folder-close", "fa-solid fa-folder" },
            { "folder-open", "fa-solid fa-folder-open" }, { "resize-vertical", "fa-solid fa-up-down" }, { "resize-horizontal", "fa-solid fa-left-right" }, { "hdd", "fa-solid fa-hard-drive" }, { "bullhorn", "fa-solid fa-bullhorn" },
            { "bell", "fa-solid fa-bell" }, { "certificate", "fa-solid fa-certificate" }, { "thumbs-up", "fa-solid fa-thumbs-up" }, { "thumbs-down", "fa-solid fa-thumbs-down" }, { "hand-right", "fa-solid fa-hand-point-right" },
            { "hand-left", "fa-solid fa-hand-point-left" }, { "hand-up", "fa-solid fa-hand-point-up" }, { "hand-down", "fa-solid fa-hand-point-down" }, { "circle-arrow-right", "fa-solid fa-circle-arrow-right" }, { "circle-arrow-left", "fa-solid fa-circle-arrow-left" },
            { "circle-arrow-up", "fa-solid fa-circle-arrow-up" }, { "circle-arrow-down", "fa-solid fa-circle-arrow-down" }, { "globe", "fa-solid fa-globe" }, { "wrench", "fa-solid fa-wrench" }, { "tasks", "fa-solid fa-list-check" },
            { "filter", "fa-solid fa-filter" }, { "briefcase", "fa-solid fa-briefcase" }, { "fullscreen", "fa-solid fa-expand" }, { "dashboard", "fa-solid fa-gauge" }, { "paperclip", "fa-solid fa-paperclip" },
            { "heart-empty", "fa-regular fa-heart" }, { "link", "fa-solid fa-link" }, { "phone", "fa-solid fa-phone" }, { "pushpin", "fa-solid fa-thumbtack" }, { "usd", "fa-solid fa-dollar-sign" },
            { "gbp", "fa-solid fa-sterling-sign" }, { "sort", "fa-solid fa-sort" }, { "sort-by-alphabet", "fa-solid fa-arrow-down-a-z" }, { "sort-by-alphabet-alt", "fa-solid fa-arrow-up-z-a" }, { "sort-by-order", "fa-solid fa-arrow-down-1-9" },
            { "sort-by-order-alt", "fa-solid fa-arrow-up-9-1" }, { "sort-by-attributes", "fa-solid fa-arrow-down-short-wide" }, { "sort-by-attributes-alt", "fa-solid fa-arrow-up-wide-short" }, { "unchecked", "fa-regular fa-square" }, { "expand", "fa-solid fa-chevron-right" },
            { "collapse-down", "fa-solid fa-chevron-down" }, { "collapse-up", "fa-solid fa-chevron-up" }, { "log-in", "fa-solid fa-right-to-bracket" }, { "flash", "fa-solid fa-bolt" }, { "log-out", "fa-solid fa-right-from-bracket" },
            { "new-window", "fa-solid fa-up-right-from-square" }, { "record", "fa-solid fa-circle" }, { "save", "fa-regular fa-floppy-disk" }, { "open", "fa-solid fa-folder-open" }, { "saved", "fa-solid fa-circle-check" },
            { "import", "fa-solid fa-file-import" }, { "export", "fa-solid fa-file-export" }, { "send", "fa-solid fa-paper-plane" }, { "floppy-disk", "fa-regular fa-floppy-disk" }, { "floppy-saved", "fa-solid fa-floppy-disk" },
            { "floppy-remove", "fa-solid fa-floppy-disk" }, { "floppy-save", "fa-solid fa-floppy-disk" }, { "floppy-open", "fa-regular fa-floppy-disk" }, { "credit-card", "fa-regular fa-credit-card" }, { "transfer", "fa-solid fa-right-left" },
            { "cutlery", "fa-solid fa-utensils" }, { "header", "fa-solid fa-heading" }, { "compressed", "fa-solid fa-file-zipper" }, { "earphone", "fa-solid fa-phone" }, { "phone-alt", "fa-solid fa-phone-flip" },
            { "tower", "fa-solid fa-tower-broadcast" }, { "stats", "fa-solid fa-chart-column" }, { "sd-video", "fa-solid fa-video" }, { "hd-video", "fa-solid fa-video" }, { "subtitles", "fa-solid fa-closed-captioning" },
            { "sound-stereo", "fa-solid fa-volume-high" }, { "sound-dolby", "fa-solid fa-volume-high" }, { "sound-5-1", "fa-solid fa-volume-high" }, { "sound-6-1", "fa-solid fa-volume-high" }, { "sound-7-1", "fa-solid fa-volume-high" },
            { "copyright-mark", "fa-regular fa-copyright" }, { "registration-mark", "fa-regular fa-registered" }, { "cloud-download", "fa-solid fa-cloud-arrow-down" }, { "cloud-upload", "fa-solid fa-cloud-arrow-up" }, { "tree-conifer", "fa-solid fa-tree" },
            { "tree-deciduous", "fa-solid fa-tree" }, { "cd", "fa-solid fa-compact-disc" }, { "save-file", "fa-regular fa-floppy-disk" }, { "open-file", "fa-solid fa-folder-open" }, { "level-up", "fa-solid fa-turn-up" },
            { "copy", "fa-regular fa-copy" }, { "paste", "fa-regular fa-paste" }, { "alert", "fa-solid fa-triangle-exclamation" }, { "equalizer", "fa-solid fa-sliders" }, { "king", "fa-solid fa-chess-king" },
            { "queen", "fa-solid fa-chess-queen" }, { "pawn", "fa-solid fa-chess-pawn" }, { "bishop", "fa-solid fa-chess-bishop" }, { "knight", "fa-solid fa-chess-knight" }, { "baby-formula", "fa-solid fa-bottle-water" },
            { "tent", "fa-solid fa-campground" }, { "blackboard", "fa-solid fa-chalkboard" }, { "bed", "fa-solid fa-bed" }, { "apple", "fa-solid fa-apple-whole" }, { "erase", "fa-solid fa-eraser" },
            { "hourglass", "fa-regular fa-hourglass" }, { "lamp", "fa-solid fa-lightbulb" }, { "duplicate", "fa-regular fa-clone" }, { "piggy-bank", "fa-solid fa-piggy-bank" }, { "scissors", "fa-solid fa-scissors" },
            { "bitcoin", "fa-brands fa-bitcoin" }, { "yen", "fa-solid fa-yen-sign" }, { "ruble", "fa-solid fa-ruble-sign" }, { "scale", "fa-solid fa-scale-balanced" }, { "ice-lolly", "fa-solid fa-ice-cream" },
            { "ice-lolly-tasted", "fa-solid fa-ice-cream" }, { "education", "fa-solid fa-graduation-cap" }, { "option-horizontal", "fa-solid fa-ellipsis" }, { "option-vertical", "fa-solid fa-ellipsis-vertical" }, { "menu-hamburger", "fa-solid fa-bars" },
            { "modal-window", "fa-regular fa-window-maximize" }, { "oil", "fa-solid fa-oil-can" }, { "grain", "fa-solid fa-wheat-awn" }, { "sunglasses", "fa-solid fa-glasses" }, { "text-size", "fa-solid fa-text-height" },
            { "text-color", "fa-solid fa-palette" }, { "text-background", "fa-solid fa-highlighter" }, { "object-align-top", "fa-solid fa-arrows-up-to-line" }, { "object-align-bottom", "fa-solid fa-arrows-down-to-line" }, { "object-align-horizontal", "fa-solid fa-arrows-left-right" },
            { "object-align-left", "fa-solid fa-align-left" }, { "object-align-vertical", "fa-solid fa-arrows-up-down" }, { "object-align-right", "fa-solid fa-align-right" }, { "triangle-right", "fa-solid fa-caret-right" }, { "triangle-left", "fa-solid fa-caret-left" },
            { "triangle-bottom", "fa-solid fa-caret-down" }, { "triangle-top", "fa-solid fa-caret-up" }, { "console", "fa-solid fa-terminal" }, { "superscript", "fa-solid fa-superscript" }, { "subscript", "fa-solid fa-subscript" },
            { "menu-left", "fa-solid fa-caret-left" }, { "menu-right", "fa-solid fa-caret-right" }, { "menu-down", "fa-solid fa-caret-down" }, { "menu-up", "fa-solid fa-caret-up" }
        };

        /// <summary>
        /// Distinct, sorted list of Font Awesome 6 icon classes available as icon enum values.
        /// </summary>
        public static string[] FontAwesomeIcons = GlyphiconToFontAwesome.Values.Distinct().OrderBy(i => i).ToArray();

        /// <summary>
        /// Resolves a stored icon value to a Font Awesome 6 class. Values already in Font Awesome
        /// form (containing "fa-") are returned unchanged; legacy Bootstrap 3 Glyphicon names
        /// (optionally prefixed with "glyphicon-") are mapped; anything unknown falls back to a default icon.
        /// </summary>
        public static string GetFontAwesomeIcon(string storedIcon)
        {
            if (string.IsNullOrWhiteSpace(storedIcon)) return "";
            var icon = storedIcon.Trim();
            //Already a Font Awesome class
            if (icon.Contains("fa-")) return icon;
            //Strip a legacy "glyphicon glyphicon-name" / "glyphicon-name" prefix
            icon = icon.Replace("glyphicon-", "").Replace("glyphicon", "").Trim();
            if (GlyphiconToFontAwesome.TryGetValue(icon, out var fa)) return fa;
            return "fa-solid fa-circle-info";
        }

#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("Value").SetIsBrowsable(Type == ViewParameterType.String);
                GetProperty("TextValue").SetIsBrowsable(Type == ViewParameterType.Text);
                GetProperty("BoolValue").SetIsBrowsable(Type == ViewParameterType.Boolean);
                GetProperty("NumericValue").SetIsBrowsable(Type == ViewParameterType.Numeric);
                GetProperty("EnumValue").SetIsBrowsable(Type == ViewParameterType.Enum);
                GetProperty("DoubleValue").SetIsBrowsable(Type == ViewParameterType.Double);
                GetProperty("Description").SetIsBrowsable(true);
                GetProperty("HelperResetParameterValue").SetIsBrowsable(true);

                //Read only
                GetProperty("Description").SetIsReadOnly(true);

                if (this is OutputParameter)
                {
                    GetProperty("CustomValue").SetIsBrowsable(true);
                    GetProperty("Value").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("TextValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("BoolValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("NumericValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("DoubleValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                    GetProperty("EnumValue").SetIsReadOnly(!((OutputParameter)this).CustomValue);
                }

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

#endif
        /// <summary>
        /// Parameter type
        /// </summary>
        [XmlIgnore]
        public ViewParameterType Type { get; set; } = ViewParameterType.Text;

        /// <summary>
        /// If true and the parameter is an enum, only the enum values defined can be selected
        /// </summary>
        [XmlIgnore]
        public bool UseOnlyEnumValues { get; set; } = true;

        /// <summary>
        /// The parameter display name
        /// </summary>
#if WINDOWS
        [DisplayName("Name"), Description("The parameter display name."), Category("Definition")]
#endif
        [XmlIgnore]
        public string DisplayName { get; set; }

        /// <summary>
        /// The parameter description
        /// </summary>
#if WINDOWS
        [DisplayName("Description"), Description("The parameter description."), Category("Helpers")]
#endif
        [XmlIgnore]
        public string Description { get; set; }

        /// <summary>
        /// The parameter value
        /// </summary>
#if WINDOWS
        [DisplayName("Value"), Description("The parameter value."), Category("Definition")]
#endif
        public string Value { get; set; } = "";

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public string EditorLanguage { get; set; } = "";

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public string[] TextSamples { get; set; } = null;

        /// <summary>
        /// If true, the parameter value can be modified in the report result dialog
        /// </summary>
        [XmlIgnore]
        public bool ShowInResultOptions { get; set; } = false;

        /// <summary>
        /// Name when used as a result option paramter
        /// </summary>
        [XmlIgnore]
        public string ResultOptionName { get; set; }

        /// <summary>
        /// Display name when used as a result option paramter
        /// </summary>
        [XmlIgnore]
        public string ResultOptionDisplayName { get; set; }

        /// <summary>
        /// The boolean parameter value
        /// </summary>
#if WINDOWS
        [DisplayName("Value"), Description("The boolean parameter value."), Category("Definition")]
#endif
        [XmlIgnore]
        public bool BoolValue
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Type != ViewParameterType.Boolean) return false;
                if (bool.TryParse(Value, out bool boolValue)) return boolValue;
                return false;
            }
            set
            {
                Type = ViewParameterType.Boolean;
                Value = value.ToString();
            }
        }

        /// <summary>
        /// The numeric parameter value
        /// </summary>
#if WINDOWS
        [DisplayName("Value"), Description("The numeric parameter value."), Category("Definition")]
#endif
        [XmlIgnore]
        public int NumericValue
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Type != ViewParameterType.Numeric) return 0;
                if (int.TryParse(Value, out int intValue)) return intValue;
                return 0;
            }
            set
            {
                Type = ViewParameterType.Numeric;
                Value = value.ToString();
            }
        }

        /// <summary>
        /// The double parameter value
        /// </summary>
#if WINDOWS
        [DisplayName("Value"), Description("The double parameter value."), Category("Definition")]
#endif
        [XmlIgnore]
        public double DoubleValue
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Type != ViewParameterType.Double) return 0;
                if (double.TryParse(Value, CultureInfo.InvariantCulture, out double doubleValue)) return doubleValue;
                return 0;
            }
            set
            {
                Type = ViewParameterType.Double;
                Value = value.ToString(CultureInfo.InvariantCulture);
            }
        }
        /// <summary>
                 /// The text parameter value
                 /// </summary>
#if WINDOWS
        [DisplayName("Value"), Description("The text parameter value."), Category("Definition")]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string TextValue
        {
            get
            {
                return Value;
            }
            set
            {
                Type = ViewParameterType.Text;
                Value = value;
            }
        }


        private string[] _enums = null;
        /// <summary>
        /// List of string values if the parameter is an enum. Each enum can have an id and an optional display. 
        /// </summary>
        [XmlIgnore]
        public string[] Enums
        {
            get
            {
                if (_enums == null) return null;

                var vals = new List<string>(_enums);
                if (_enumType != null)
                {
                    foreach (var val in _enumType.GetEnumValues())
                    {
                        //Add names
                        var desc = Helper.GetEnumDescription(EnumType, val);
                        if(desc == val.ToString()) desc = Helper.DBNameToDisplayName(desc);
                        vals.Add(val.ToString() + "|" + desc);
                    }
                }

                return vals.ToArray();
            }
            set
            {
                if (value != null) Type = ViewParameterType.Enum;
                _enums = value;
            }
        }

        /// <summary>
        /// If set, the enum values are taken from the Enum defined
        /// </summary>
        Type _enumType = null;

        public Type EnumType
        {
            get
            {
                return _enumType;
            }
            set
            {
                if (value != null) Type = ViewParameterType.Enum;
                _enumType = value;

            }
        }

        /// <summary>
        /// List of enum values if the parameter is an enum
        /// </summary>
        [XmlIgnore]
        public string[] EnumValues
        {
            get
            {
                List<string> result = new List<string>();
                foreach (var val in _enums)
                {
                    result.Add(val.Contains("|") ? val.Split('|')[0] : val);
                }

                return result.ToArray();
            }
        }

        /// <summary>
        /// List of enum display values if the parameter is an enum
        /// </summary>
        [XmlIgnore]
        public string[] EnumDisplays
        {
            get
            {
                List<string> result = new List<string>();
                foreach (var val in _enums)
                {
                    result.Add(val.Contains("|") ? val.Split('|')[1] : val);
                }
                return result.ToArray();
            }
        }

        /// <summary>
        /// The enum parameter value
        /// </summary>
#if WINDOWS
        [DisplayName("Value"), Description("The parameter value."), Category("Definition")]
        [TypeConverter(typeof(ViewParameterEnumConverter))]
#endif
        [XmlIgnore]
        public string EnumValue
        {
            get
            {
                return Value != null && Value.Contains("|") ? Value.Split('|')[0] : Value;
            }
            set
            {
                Value = value;
            }
        }

        [XmlIgnore]
        public string[] MultipleEnumValues
        {
            get
            {
                return Value == null ? [] : Value.Split('|');
            }
        }

        /// <summary>
        /// String to store the default configuration value
        /// </summary>
        [XmlIgnore]
        public string ConfigValue = "";

        /// <summary>
        /// Default configuration value
        /// </summary>
        [XmlIgnore]
        public object ConfigObject
        {
            get
            {
                if (Type == ViewParameterType.Boolean)
                {
                    if (string.IsNullOrEmpty(ConfigValue)) return false;
                    return bool.Parse(ConfigValue);
                }
                else if (Type == ViewParameterType.Numeric)
                {
                    if (string.IsNullOrEmpty(ConfigValue)) return 0;
                    return int.Parse(ConfigValue);
                }
                else if (Type == ViewParameterType.Double)
                {
                    if (string.IsNullOrEmpty(ConfigValue)) return 0;
                    return double.Parse(ConfigValue, CultureInfo.InvariantCulture);
                }
                return ConfigValue;
            }
        }

        /// <summary>
        /// Editor Helper: Reset parameter to its default value
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Reset value"), Description("Reset parameter to its default value.")]
        [Editor(typeof(HelperEditor), typeof(UITypeEditor))]
#endif
        public string HelperResetParameterValue
        {
            get { return "<Click to reset to the default value>"; }
        }

        /// <summary>
        /// For an enum, returns the display text from the value
        /// </summary>
        public string EnumGetDisplayFromValue(string value)
        {
            int index = EnumValues.ToList().FindIndex(i => i == value);
            if (index >= 0 && index < EnumDisplays.Length) return EnumDisplays[index];
            return value;
        }

        /// <summary>
        /// For an enum, returns the value from the display text
        /// </summary>
        public string EnumGetValueFromDisplay(string display)
        {
            int index = EnumDisplays.ToList().FindIndex(i => i == display);
            if (index >= 0 && index < EnumValues.Length) return EnumValues[index];
            return display;
        }

        /// <summary>
        /// Init parameter values from a reference 
        /// </summary>
        public void InitFromConfiguration(Parameter configuration)
        {
            Name = configuration.Name;
            Enums = configuration.Enums;
            Description = configuration.Description;
            Type = configuration.Type;
            UseOnlyEnumValues = configuration.UseOnlyEnumValues;
            ShowInResultOptions = configuration.ShowInResultOptions;
            DisplayName = configuration.DisplayName;
            ConfigValue = configuration.Value;
            EditorLanguage = configuration.EditorLanguage;
            TextSamples = configuration.TextSamples;
        }
    }

    /// <summary>
    /// OutputParameter are Parameter used for report output
    /// </summary>
    public class OutputParameter : Parameter
    {
        bool _customValue = false;
        /// <summary>
        /// If true, a custom parameter value is used when the report is executed for the output
        /// </summary>
#if WINDOWS
        [DisplayName("Use custom value"), Description("If true, a custom parameter value is used when the report is executed for the output."), Category("Definition")]
        [DefaultValue(false)]
#endif
        public bool CustomValue
        {
            get
            {
                return _customValue;
            }
            set
            {
                _customValue = value;
                UpdateEditorAttributes();
            }
        }
    }

    /// <summary>
    /// SecurityParameter are Parameters used to define the security
    /// </summary>
    public class SecurityParameter : Parameter
    {
    }
}

