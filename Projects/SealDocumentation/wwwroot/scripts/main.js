function executeReport(path) {
    var server = "https://sealreport.org/demo/";
    fetch(server + "SWILogin", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: new URLSearchParams({
            user: "", // The user name
            password: "" // The password
        })
    }).then(function () {
        var form = document.createElement("form");
        form.method = "post";
        form.target = "_blank";
        form.action = server + "SWExecuteReport";
        var input = document.createElement("input");
        input.type = "hidden";
        input.name = "path"; // The report path, if empty the report definition must be specified
        input.value = path;
        form.appendChild(input);
        document.body.appendChild(form);
        form.submit();
        form.remove();
    });
    return false;
}

document.addEventListener("DOMContentLoaded", function () {

    // Highlight the current page in the top navigation bar
    var currentPath = location.pathname.replace(/\/$/, "").toLowerCase();
    document.querySelectorAll("#bar_top .navbar-nav a.nav-link[id]").forEach(function (link) {
        var linkPath = new URL(link.href).pathname.replace(/\/$/, "").toLowerCase();
        if (linkPath === currentPath || (linkPath === "" && (currentPath === "" || currentPath === "/index"))) {
            link.classList.add("active");
            link.setAttribute("aria-current", "page");
        }
    });

    // Live sample links: execute the report on the demo server
    function bindLiveSample(selector, prefix, root) {
        document.querySelectorAll(selector).forEach(function (el) {
            el.addEventListener("click", function () {
                executeReport(el.textContent.replace(prefix, root) + ".srex");
            });
        });
    }
    bindLiveSample(".live-sample-root", "Live Sample: ", "/");
    bindLiveSample(".live-sample", "Live Sample: ", "/Samples/");
    bindLiveSample(".live-system", "System: ", "/System/");

    // Sidebar scrollspy: highlight the section currently on screen
    var sidebar = document.querySelector(".docs-sidebar");
    if (sidebar) {
        var links = Array.prototype.slice.call(sidebar.querySelectorAll("a[href^='#']"));
        var items = links.map(function (link) {
            return { link: link, target: document.getElementById(link.getAttribute("href").substring(1)) };
        }).filter(function (item) { return item.target; });

        var setActive = function (link) {
            sidebar.querySelectorAll("li.active").forEach(function (li) { li.classList.remove("active"); });
            if (!link) return;
            var li = link.closest("li");
            while (li) {
                li.classList.add("active");
                li = li.parentElement ? li.parentElement.closest("li") : null;
            }
        };

        var onScroll = function () {
            var position = window.scrollY + 90;
            var current = null;
            items.forEach(function (item) {
                if (item.target.getBoundingClientRect().top + window.scrollY <= position) current = item.link;
            });
            setActive(current);
        };

        var ticking = false;
        window.addEventListener("scroll", function () {
            if (ticking) return;
            ticking = true;
            window.requestAnimationFrame(function () { onScroll(); ticking = false; });
        });
        onScroll();
    }

    // Back to top button
    var backToTop = document.getElementById("back-to-top");
    if (backToTop) {
        window.addEventListener("scroll", function () {
            backToTop.classList.toggle("show", window.scrollY > 50);
        });
        backToTop.addEventListener("click", function (e) {
            e.preventDefault();
            window.scrollTo({ top: 0, behavior: "smooth" });
        });
    }
});
