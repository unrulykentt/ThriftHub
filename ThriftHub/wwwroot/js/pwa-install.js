(function () {

    const banner =
        document.getElementById("thPwaInstallBanner");

    if (!banner) {
        return;
    }

    const dismissKey =
        "thrifthub_pwa_banner_dismissed";

    const iosGuideUrl =
        banner.dataset.iosGuideUrl || "/Home/InstallApp";

    const isStandalone =
        window.matchMedia("(display-mode: standalone)").matches ||
        window.navigator.standalone === true;

    if (isStandalone) {
        banner.hidden = true;
        return;
    }

    if (localStorage.getItem(dismissKey) === "1") {
        banner.hidden = true;
        return;
    }

    const closeButton =
        banner.querySelector("[data-pwa-dismiss]");

    const installButton =
        banner.querySelector("[data-pwa-install]");

    const guideButton =
        banner.querySelector("[data-pwa-guide]");

    const iosHint =
        banner.querySelector("[data-pwa-ios-hint]");

    const androidHint =
        banner.querySelector("[data-pwa-android-hint]");

    const userAgent =
        window.navigator.userAgent || "";

    const isMobile =
        /Android|iPhone|iPad|iPod|Mobile/i.test(userAgent);

    if (!isMobile) {
        banner.hidden = true;
        return;
    }

    const isIos =
        /iPad|iPhone|iPod/.test(userAgent) &&
        !window.MSStream;

    const isAndroid =
        /Android/.test(userAgent);

    let deferredPrompt = null;

    if (isIos) {
        if (iosHint) {
            iosHint.hidden = false;
        }

        if (guideButton) {
            guideButton.hidden = false;
            guideButton.href = iosGuideUrl;
        }

        if (installButton) {
            installButton.hidden = true;
        }
    }
    else if (isAndroid) {
        if (androidHint) {
            androidHint.hidden = false;
        }
    }
    else if (androidHint) {
        androidHint.hidden = false;
    }

    window.addEventListener("beforeinstallprompt", (event) => {
        event.preventDefault();
        deferredPrompt = event;

        if (installButton) {
            installButton.hidden = false;
        }

        if (androidHint) {
            androidHint.textContent =
                "Tap Install below, or use your browser menu.";
        }
    });

    if (closeButton) {
        closeButton.addEventListener("click", () => {
            banner.hidden = true;
            localStorage.setItem(dismissKey, "1");
        });
    }

    if (guideButton) {
        guideButton.addEventListener("click", () => {
            localStorage.setItem(dismissKey, "1");
        });
    }

    if (installButton) {
        installButton.addEventListener("click", async () => {
            if (!deferredPrompt) {
                window.location.href = iosGuideUrl;
                return;
            }

            deferredPrompt.prompt();

            await deferredPrompt.userChoice;

            deferredPrompt = null;
            banner.hidden = true;
            localStorage.setItem(dismissKey, "1");
        });
    }

    if ("serviceWorker" in navigator) {
        window.addEventListener("load", () => {
            navigator.serviceWorker
                .register("/sw.js")
                .catch(() => {
                    // Service worker is optional.
                });
        });
    }

})();
