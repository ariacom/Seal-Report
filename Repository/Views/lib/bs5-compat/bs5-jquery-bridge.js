/*!
 * Bootstrap 5 jQuery compatibility bridge for Seal Report.
 *
 * Bootstrap 5 dropped the jQuery plugin API ($.fn.modal, $.fn.tooltip, ...) and
 * now dispatches native events. A lot of Seal Report / Seal Web Interface code
 * still calls the jQuery-style plugins ($('#x').modal('show')) and binds the
 * namespaced events ($el.on('hidden.bs.modal')). This shim re-adds those plugins
 * on top of Bootstrap 5's vanilla API and re-dispatches the component events
 * through jQuery so existing handlers keep firing.
 *
 * Must load AFTER jQuery and bootstrap.bundle, and BEFORE the application scripts.
 */
(function (factory) {
    function boot() {
        if (typeof window.jQuery !== "undefined" && typeof window.bootstrap !== "undefined") {
            factory(window.jQuery, window.bootstrap);
        }
    }
    // Bootstrap 5 attaches its own jQuery plugins ($.fn.modal = jQueryInterface) on
    // DOMContentLoaded, and that interface ignores the no-argument call (".modal()" only
    // inits, never shows). We must install our shim AFTER Bootstrap's deferred attach, so
    // defer to DOMContentLoaded as well (our handler registers after Bootstrap's, thus wins).
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})(function ($, bootstrap) {
    "use strict";

    // Map a jQuery plugin name to its Bootstrap 5 component, with the method used
    // when the plugin is called with no arguments (BS3 behavior).
    var components = {
        modal: { ctor: bootstrap.Modal, def: "show" },
        offcanvas: { ctor: bootstrap.Offcanvas, def: "show" },
        tab: { ctor: bootstrap.Tab, def: "show" },
        collapse: { ctor: bootstrap.Collapse, def: "toggle" },
        dropdown: { ctor: bootstrap.Dropdown, def: "toggle" },
        tooltip: { ctor: bootstrap.Tooltip, def: null },
        popover: { ctor: bootstrap.Popover, def: null },
        alert: { ctor: bootstrap.Alert, def: null },
        toast: { ctor: bootstrap.Toast, def: "show" },
        button: { ctor: bootstrap.Button, def: "toggle" }
    };

    // BS3/4 method names that changed in BS5.
    var methodAliases = { destroy: "dispose" };

    Object.keys(components).forEach(function (name) {
        var def = components[name];
        if (!def.ctor) return;

        $.fn[name] = function (arg, relatedTarget) {
            var options = (arg && typeof arg === "object") ? arg : {};
            var method = (typeof arg === "string") ? (methodAliases[arg] || arg) : def.def;

            this.each(function () {
                var instance = def.ctor.getOrCreateInstance(this, options);
                if (method && typeof instance[method] === "function") {
                    instance[method](relatedTarget);
                }
            });
            return this; // keep jQuery chaining
        };

        // Bootstrap exposes the component class (with its static .Default config) as
        // $.fn[name].Constructor. Plugins such as bootstrap-select read
        // $.fn.dropdown.Constructor.Default, which would be undefined once we override the
        // plugin, so re-expose it here.
        $.fn[name].Constructor = def.ctor;
    });

    // bootstrap-select 1.14 + Bootstrap 5: its virtual-scroll windowing miscalculates the
    // inner menu margins (e.g. margin-top: 19232px), pushing the options off-screen so the
    // menu appears blank until scrolled. The Seal Report lists are small, so disable virtual
    // scrolling globally; all options then render in normal flow.
    if ($.fn.selectpicker && $.fn.selectpicker.Constructor && $.fn.selectpicker.Constructor.DEFAULTS) {
        $.fn.selectpicker.Constructor.DEFAULTS.virtualScroll = false;
    }

    // Re-dispatch Bootstrap 5 native component events through jQuery so that
    // handlers bound with namespaced names ($el.on('hidden.bs.modal', ...)) fire.
    var events = ["show", "shown", "hide", "hidden", "hidePrevented",
                  "loaded", "slid", "slide", "inserted"];
    var names = Object.keys(components).concat(["carousel"]);
    var seen = {};
    names.forEach(function (c) {
        events.forEach(function (e) {
            var type = e + ".bs." + c;
            if (seen[type]) return;
            seen[type] = true;
            document.addEventListener(type, function (ev) {
                var $target = $(ev.target);
                // triggerHandler runs the jQuery handlers directly (no native re-dispatch),
                // so this cannot loop back into this listener.
                $target.triggerHandler(type, [ev]);
            }, false);
        });
    });
});
