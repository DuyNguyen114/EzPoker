using Microsoft.Extensions.Logging;
using PokerTrainer.Services;

namespace PokerTrainer.Mobile
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


            //add service
            builder.Services.AddSingleton<IBettingService, BettingService>();

            builder.Services.AddSingleton<ITurnOrderService, TurnOrderService>();

            builder.Services.AddSingleton<IPokerGameService, PokerGameService>();

            builder.Services.AddSingleton<HandLifecycleService>();
#endif

            return builder.Build();
        }
    }
}
