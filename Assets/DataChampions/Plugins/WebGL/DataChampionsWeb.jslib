mergeInto(LibraryManager.library, {
  // Caps the web player's frame rate while keeping requestAnimationFrame (vsync) pacing.
  // Unity's Application.targetFrameRate switches the main loop to setTimeout, which drifts against
  // the display and stutters; instead this skips vsync ticks until the next frame slot is due.
  DC_SetFrameCap__deps: ['$Browser'],
  DC_SetFrameCap: function (fps) {
    if (!Browser.dcNativeRaf) Browser.dcNativeRaf = Browser.requestAnimationFrame;
    var nativeRaf = Browser.dcNativeRaf;
    if (fps <= 0) {
      Browser.requestAnimationFrame = nativeRaf;
      return;
    }
    var interval = 1000 / fps;
    var tolerance = 2; // ms; rAF timestamps jitter slightly around the refresh interval
    var next = 0;
    Browser.requestAnimationFrame = function (func) {
      nativeRaf(function tick(t) {
        if (t < next - tolerance) {
          nativeRaf(tick);
          return;
        }
        next += interval;
        if (next < t) next = t + interval; // after a slow frame or a hidden tab, don't try to catch up
        func(t);
      });
    };
  },

  // Hands progress to the page: postMessage to the parent frame (LMS wrapper) and a DOM event.
  DC_Report: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);
    var data;
    try { data = JSON.parse(json); } catch (e) { data = { raw: json }; }
    try { if (window.parent && window.parent !== window) window.parent.postMessage({ type: 'datachampions-progress', data: data }, '*'); } catch (e) {}
    try { window.dispatchEvent(new CustomEvent('datachampions-progress', { detail: data })); } catch (e) {}
    console.log('[DataChampions] progress', data);
  }
});
