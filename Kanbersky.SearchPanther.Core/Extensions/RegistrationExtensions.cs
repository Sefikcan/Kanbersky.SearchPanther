using Kanbersky.SearchPanther.Core.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Kanbersky.SearchPanther.Core.Extensions
{
    public static class RegistrationExtensions
    {
        public static void UseExceptionMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
