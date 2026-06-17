/*!
 * Bootstrap 5 integration for bootstrap-select (1.14) in Seal Report.
 *
 * bootstrap-select is a jQuery plugin originally written for Bootstrap 3/4. It runs on
 * Bootstrap 5 (its core toggle uses the native bootstrap.Dropdown and it detects the
 * version from bootstrap.Dropdown.VERSION), but two BS5-specific issues remain and are
 * fixed here. This is NOT a Bootstrap 3 compatibility shim: it only configures and patches
 * the bootstrap-select dependency so it behaves correctly under Bootstrap 5.
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
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})();
