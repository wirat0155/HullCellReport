using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HullCellReport.Middlewares
{
    public class RedirectUnauthorizedMiddleware
    {
        private readonly RequestDelegate _next;

        public RedirectUnauthorizedMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);
            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                var loginPath = $"{context.Request.PathBase}/Auth/vLogin";
                context.Response.Redirect(loginPath);
            }
        }
    }
}
