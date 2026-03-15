mergeInto(LibraryManager.library, {

    // Détecte si l'appareil est mobile/tactile via userAgent et maxTouchPoints
    JS_IsMobileDevice: function () {
        var ua = navigator.userAgent || "";
        var isMobileUA = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(ua);
        var hasTouch = (navigator.maxTouchPoints && navigator.maxTouchPoints > 2);
        return (isMobileUA || hasTouch) ? 1 : 0;
    },

    // Demande le plein écran sur l'élément canvas (nécessite un geste utilisateur)
    JS_RequestFullscreen: function () {
        var canvas = document.getElementById("unity-canvas") || document.querySelector("canvas");
        var el = canvas || document.documentElement;

        if (el.requestFullscreen) {
            el.requestFullscreen().catch(function () {});
        } else if (el.webkitRequestFullscreen) {
            el.webkitRequestFullscreen();
        } else if (el.mozRequestFullScreen) {
            el.mozRequestFullScreen();
        }
    },

    // Ajoute les meta tags et styles pour maximiser l'espace sur mobile
    // (cache la barre d'adresse en forçant 100dvh et en mode standalone)
    JS_SetupMobileViewport: function () {
        // Meta viewport si absent
        if (!document.querySelector('meta[name="viewport"]')) {
            var meta = document.createElement("meta");
            meta.name = "viewport";
            meta.content = "width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover";
            document.head.appendChild(meta);
        }

        // PWA : permet l'ajout à l'écran d'accueil sans barre navigateur
        var metaApple = document.createElement("meta");
        metaApple.name = "apple-mobile-web-app-capable";
        metaApple.content = "yes";
        document.head.appendChild(metaApple);

        var metaAndroid = document.createElement("meta");
        metaAndroid.name = "mobile-web-app-capable";
        metaAndroid.content = "yes";
        document.head.appendChild(metaAndroid);

        // Forcer le canvas à remplir tout l'écran y compris les encoches
        var style = document.createElement("style");
        style.textContent = [
            "html, body { margin: 0; padding: 0; overflow: hidden; background: #000; }",
            "#unity-canvas, canvas { width: 100dvw !important; height: 100dvh !important; display: block; }"
        ].join("\n");
        document.head.appendChild(style);

        // Forcer l'orientation paysage
        function lockLandscape() {
            if (screen.orientation && screen.orientation.lock) {
                screen.orientation.lock("landscape").catch(function () {});
            } else if (screen.lockOrientation) {
                screen.lockOrientation("landscape");
            } else if (screen.mozLockOrientation) {
                screen.mozLockOrientation("landscape");
            } else if (screen.msLockOrientation) {
                screen.msLockOrientation("landscape");
            }
        }

        // Au premier toucher, plein écran + verrouillage paysage
        document.addEventListener("touchstart", function onFirstTouch() {
            document.removeEventListener("touchstart", onFirstTouch);
            var canvas = document.getElementById("unity-canvas") || document.querySelector("canvas");
            var el = canvas || document.documentElement;
            if (el.requestFullscreen) {
                el.requestFullscreen().then(lockLandscape).catch(function () {});
            } else if (el.webkitRequestFullscreen) {
                el.webkitRequestFullscreen();
                lockLandscape();
            }
        }, { once: true });
    }

});
