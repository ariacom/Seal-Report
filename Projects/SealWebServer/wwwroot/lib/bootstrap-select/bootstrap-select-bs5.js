/*!
 * Bootstrap 5 integration for bootstrap-select (1.14) in Seal Report.
 *
 * bootstrap-select is a jQuery plugin originally written for Bootstrap 3/4. It runs on
 * Bootstrap 5 (its core toggle uses the native bootstrap.Dropdown and it detects the
 * version from bootstrap.Dropdown.VERSION), but two BS5-specific issues remain and are
 * fixed here. This is NOT a Bootstrap 3 compatibility shim: it configures and patches
 * the bootstrap-select dependency so it behaves correctly under Bootstrap 5, and applies
 * one related Bootstrap 5 fix for the native combobox dropdowns in the configuration
 * dialog (relocating their menus to <body> so they escape the scrolling dialog body).
 *
 * Must load after jQuery + bootstrap.bundle. The selectpicker tweaks are applied once the
 * plugin is present (DOMContentLoaded), before any selectpicker is initialised.
 */
(function () {
    function boot() {
        var $ = window.jQuery, bootstrap = window.bootstrap;
        if (!$ || !bootstrap) return;

        // bootstrap-select 1.14 + BS5: its virtual-scroll windowing miscalculates the inner
        // menu margins (e.g. margin-top: 19232px), pushing the options off-screen so the menu
        // appears blank until scrolled. The Seal Report lists are small, so disable virtual
        // scrolling globally; all options then render in normal flow.
        if ($.fn.selectpicker && $.fn.selectpicker.Constructor && $.fn.selectpicker.Constructor.DEFAULTS) {
            $.fn.selectpicker.Constructor.DEFAULTS.virtualScroll = false;
        }

        // bootstrap-select 1.14 + BS5: bootstrap-select tries to suppress Bootstrap's own
        // keyboard handler with $(document).off('keydown.bs.dropdown.data-api'). That jQuery
        // .off() cannot remove Bootstrap 5's *native* delegated listener, so BS5's
        // Dropdown.dataApiKeydownHandler still fires while the user navigates a bootstrap-select
        // menu with the keyboard. It then fails to resolve a toggle button (the menu structure
        // isn't what BS5 expects) and calls Dropdown.getOrCreateInstance(undefined), which throws
        // "Cannot read properties of undefined (reading 'parentNode')". Guard the static factory
        // so a missing element yields a harmless no-op; bootstrap-select's own keydown handler
        // then performs the navigation.
        if (bootstrap.Dropdown && bootstrap.Dropdown.getOrCreateInstance) {
            var _getOrCreate = bootstrap.Dropdown.getOrCreateInstance;
            var _noop = {
                show: function () {}, hide: function () {}, toggle: function () {},
                _selectMenuItem: function () {}, _isShown: function () { return false; }
            };
            bootstrap.Dropdown.getOrCreateInstance = function (element, config) {
                if (!element) return _noop;
                return _getOrCreate.call(this, element, config);
            };
        }

        // The "Configuration and security" dialog scrolls its body, which clips the menus of
        // the native input-group comboboxes inside it (Login ID, Group name, Folder) when they
        // open near the bottom edge. Two things are done for each:
        //   1. Relocate the menu to <body> so it is physically outside the overflow container
        //      and can never be clipped (z-index raised above the modal). Bootstrap caches its
        //      menu element at construction and toggles the .show class on that cached node
        //      regardless of where it lives in the DOM, so moving the node keeps open/close
        //      working.
        //   2. Drive positioning ourselves instead of Popper. Popper miscomputes the horizontal
        //      offset for this reference/offset-parent combination (it lands the menu at x=0),
        //      so the dropdown is created with display:"static" (which disables Popper's inline
        //      styling) and the menu is placed with position:fixed from the toggle's viewport
        //      rect. With the menu a direct child of <body> and no transformed ancestors, fixed
        //      coordinates from getBoundingClientRect() are exact.
        if (bootstrap.Dropdown && bootstrap.Dropdown.getOrCreateInstance) {
            var positionComboMenu = function (btn, menu) {
                var r = btn.getBoundingClientRect();
                var mw = menu.offsetWidth, mh = menu.offsetHeight;
                var left = Math.max(4, r.right - mw); // right-align the menu under the toggle
                var top = r.bottom + 1;
                // flip above the toggle when there is not enough room below
                if (top + mh > window.innerHeight - 4 && r.top - mh - 1 >= 0) top = r.top - mh - 1;
                menu.style.position = "fixed";
                menu.style.inset = "auto";
                menu.style.margin = "0";
                menu.style.transform = "none";
                menu.style.left = Math.round(left) + "px";
                menu.style.top = Math.round(top) + "px";
            };
            var comboToggles = document.querySelectorAll('#config-dialog .input-group-btn button[data-bs-toggle="dropdown"]');
            Array.prototype.forEach.call(comboToggles, function (el) {
                bootstrap.Dropdown.getOrCreateInstance(el, { display: "static" }); // no Popper styling
                var menu = el.parentNode.querySelector(".dropdown-menu");
                if (!menu) return;
                if (menu.parentNode !== document.body) {
                    menu.style.zIndex = "1060"; // above the BS5 modal (1055)
                    document.body.appendChild(menu);
                }
                var reposition = function () { positionComboMenu(el, menu); };
                el.addEventListener("shown.bs.dropdown", function () {
                    reposition();
                    window.addEventListener("scroll", reposition, true);
                    window.addEventListener("resize", reposition);
                });
                el.addEventListener("hide.bs.dropdown", function () {
                    window.removeEventListener("scroll", reposition, true);
                    window.removeEventListener("resize", reposition);
                });
            });
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})();
