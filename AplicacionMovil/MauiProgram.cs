using AplicacionMovil.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

#if ANDROID
using Android.Webkit;
#endif

namespace AplicacionMovil
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

#if ANDROID
            builder.ConfigureMauiHandlers(handlers =>
            {
                WebViewHandler.Mapper.AppendToMapping("MapWebView", (handler, view) =>
                {
                    var s = handler.PlatformView.Settings;
                    s.JavaScriptEnabled = true;
                    s.DomStorageEnabled = true;

                    CookieManager.Instance.SetAcceptCookie(true);
                    CookieManager.Instance.SetAcceptThirdPartyCookies(handler.PlatformView, true);
                });
            });
#endif

            builder.Services.AddSingleton<IAuthEvents, AuthEvents>();
            builder.Services.AddTransient<AuthHandler>();

            builder.Services.AddHttpClient("api", client =>
            {
                client.BaseAddress = new Uri("https://elpuregistro.com/");
                client.Timeout = TimeSpan.FromSeconds(60);
            })
            .AddHttpMessageHandler<AuthHandler>();

            builder.Services.AddSingleton(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return factory.CreateClient("api");
            });

            return builder.Build();
        }
    }
}
