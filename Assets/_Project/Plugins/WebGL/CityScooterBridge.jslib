// Unity -> web shell half of the bridge (docs/bridge.md). The shell defines window.CityScooterShell.
mergeInto(LibraryManager.library, {
  CS_SendToDevices: function (targetPtr, linePtr) {
    var target = UTF8ToString(targetPtr);
    var line = UTF8ToString(linePtr);
    var shell = window.CityScooterShell;
    if (shell && typeof shell.sendToDevices === "function") {
      try { shell.sendToDevices(target, line); } catch (e) { console.warn("[CityScooter] sendToDevices failed", e); }
    }
  },

  CS_UnityReady: function (versionPtr) {
    var version = UTF8ToString(versionPtr);
    var shell = window.CityScooterShell;
    if (shell && typeof shell.onUnityReady === "function") {
      try { shell.onUnityReady(version); } catch (e) { console.warn("[CityScooter] onUnityReady failed", e); }
    }
  }
});
