(function () {
    const savedTheme = localStorage.getItem("temple-theme");

    if (savedTheme) {
        document.documentElement.setAttribute(
            "data-bs-theme",
            savedTheme
        );
    }

    updateThemeIcon();
})();

function toggleTheme() {
    const html = document.documentElement;

    const currentTheme =
        html.getAttribute("data-bs-theme") || "light";

    const nextTheme =
        currentTheme === "dark" ? "light" : "dark";

    html.setAttribute("data-bs-theme", nextTheme);

    localStorage.setItem(
        "temple-theme",
        nextTheme
    );

    updateThemeIcon();
}

function updateThemeIcon() {
    const icon = document.getElementById("theme-icon");

    if (!icon) {
        return;
    }

    const currentTheme =
        document.documentElement.getAttribute("data-bs-theme");

    icon.className =
        currentTheme === "dark"
            ? "ti ti-sun"
            : "ti ti-moon";
}

function toggleFullScreen() {
    if (!document.fullscreenElement) {
        document.documentElement
            .requestFullscreen()
            .catch(function () {
                // Browser may prevent fullscreen.
            });
    } else {
        document.exitFullscreen();
    }
}