using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Controls;

#if WINDOWS
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
#elif MACCATALYST
using WebKit;
#endif

namespace Starlight.Launcher;

public static class WebViewExtensions
{
    public static void PauseWebView(this BlazorWebView webView)
    {
        var platformView = webView.Handler?.PlatformView;
        if (platformView is null) return;

#if WINDOWS
        if (platformView is WebView2 wv2 && wv2.CoreWebView2 is not null)
        {
            _ = wv2.CoreWebView2.TrySuspendAsync();
            wv2.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
        }
#elif MACCATALYST
        if (platformView is WKWebView wk)
        {
            wk.SetAllMediaPlaybackSuspended(true, null);
            wk.EvaluateJavaScript(PauseJs, null);
        }
#endif
    }

    public static void ResumeWebView(this BlazorWebView webView)
    {
        var platformView = webView.Handler?.PlatformView;
        if (platformView is null) return;

#if WINDOWS
        if (platformView is WebView2 wv2 && wv2.CoreWebView2 is not null)
        {
            wv2.CoreWebView2.Resume();
            wv2.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;
        }
#elif MACCATALYST
        if (platformView is WKWebView wk)
        {
            wk.SetAllMediaPlaybackSuspended(false, null);
            wk.EvaluateJavaScript(ResumeJs, null);
        }
#endif
    }

#if MACCATALYST
    const string PauseJs = @"
        (function(){
            if (window.__wvPaused) return;
            window.__wvPaused = true;
            document.querySelectorAll('video,audio').forEach(m => m.pause());
            window.__origRAF = window.requestAnimationFrame;
            window.requestAnimationFrame = function(){ return 0; };
        })();";

    const string ResumeJs = @"
        (function(){
            if (!window.__wvPaused) return;
            window.__wvPaused = false;
            if (window.__origRAF) window.requestAnimationFrame = window.__origRAF;
        })();";
#endif
}
