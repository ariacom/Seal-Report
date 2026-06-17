//
// Copyright (c) Seal Report (sealreport.org), Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0).
//
using System.Collections.Generic;

namespace Seal.Model
{
    /// <summary>
    /// Kind of a client-side asset.
    /// </summary>
    public enum ClientAssetType
    {
        /// <summary>A CSS style sheet.</summary>
        CSS,
        /// <summary>A JavaScript file.</summary>
        Script
    }

    /// <summary>
    /// A versioned client-side asset (CSS or JavaScript library).
    /// This is the single source of truth shared by the Web Report Server shell and the report
    /// rendering engine, so a library version is declared once.
    /// Physical files are pinned and restored by libman.json into wwwroot/lib for the web application,
    /// and copied into Repository/Views/lib for report rendering.
    /// </summary>
    public class ClientAsset
    {
        /// <summary>CSS or Script.</summary>
        public ClientAssetType Type { get; set; }

        /// <summary>
        /// Path relative to the web root / Views folder, e.g. "lib/bootstrap/css/bootstrap.min.css".
        /// Resolved against wwwroot by the web application, and against Repository/Views by the rendering engine
        /// (see Report.AttachCSSPath / AttachScriptPath).
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Optional CDN URL, used by the rendering engine when the repository is not configured as local.
        /// </summary>
        public string Cdn { get; set; }

        /// <summary>
        /// Creates a client asset definition.
        /// </summary>
        public ClientAsset(ClientAssetType type, string path, string cdn = "")
        {
            Type = type;
            Path = path;
            Cdn = cdn;
        }
    }

    /// <summary>
    /// Central, ordered registry of the Bootstrap 5 client stack.
    /// Load order is significant: jQuery (currently bundled in datatables) then Bootstrap.
    /// bootstrap.bundle.min.js already includes Popper, so Popper is not loaded separately for the core.
    /// </summary>
    public static class ClientAssets
    {
        /// <summary>Bootstrap 5 core style sheet, loaded by every page.</summary>
        public static readonly List<ClientAsset> BootstrapCss = new List<ClientAsset>
        {
            new ClientAsset(ClientAssetType.CSS, "lib/bootstrap/css/bootstrap.min.css",
                "https://cdnjs.cloudflare.com/ajax/libs/bootstrap/5.3.3/css/bootstrap.min.css")
        };

        /// <summary>
        /// Bootstrap 5 core JavaScript: the bundle (includes Popper) followed by the
        /// bootstrap-select Bootstrap 5 integration (configures the selectpicker dependency
        /// and patches two BS5-specific bootstrap-select bugs). The application code calls the
        /// native bootstrap.* API directly, so no jQuery-plugin compatibility bridge is loaded.
        /// The integration must load right after the bundle and before the application scripts.
        /// </summary>
        public static readonly List<ClientAsset> BootstrapJs = new List<ClientAsset>
        {
            new ClientAsset(ClientAssetType.Script, "lib/bootstrap/js/bootstrap.bundle.min.js",
                "https://cdnjs.cloudflare.com/ajax/libs/bootstrap/5.3.3/js/bootstrap.bundle.min.js"),
            new ClientAsset(ClientAssetType.Script, "lib/bootstrap-select/bootstrap-select-bs5.js")
        };

        /// <summary>Flatpickr style sheet (lightweight Bootstrap-5-compatible date/time picker, replaces bootstrap-datetimepicker).</summary>
        public static readonly List<ClientAsset> FlatpickrCss = new List<ClientAsset>
        {
            new ClientAsset(ClientAssetType.CSS, "lib/flatpickr/flatpickr.min.css",
                "https://cdnjs.cloudflare.com/ajax/libs/flatpickr/4.6.13/flatpickr.min.css")
        };

        /// <summary>
        /// Flatpickr JavaScript (date/time picker). Dependency-free (no jQuery/moment), exposes the global
        /// "flatpickr". Localization comes from per-language l10n files loaded on demand by the views.
        /// </summary>
        public static readonly List<ClientAsset> FlatpickrJs = new List<ClientAsset>
        {
            new ClientAsset(ClientAssetType.Script, "lib/flatpickr/flatpickr.min.js",
                "https://cdnjs.cloudflare.com/ajax/libs/flatpickr/4.6.13/flatpickr.min.js")
        };

        /// <summary>
        /// Font Awesome 6 (Free) style sheets. all.min.css provides the icons.
        /// </summary>
        public static readonly List<ClientAsset> FontAwesomeCss = new List<ClientAsset>
        {
            new ClientAsset(ClientAssetType.CSS, "lib/fontawesome/css/all.min.css",
                "https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.7.2/css/all.min.css")
        };
    }
}
