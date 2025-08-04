using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace soft20181_starter.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                
                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(@"
                        <html>
                            <head>
                                <title>Error - EventScape</title>
                                <style>
                                    body {
                                        font-family: Arial, sans-serif;
                                        margin: 0;
                                        padding: 20px;
                                        background: #f8f9fa;
                                    }
                                    .error-container {
                                        max-width: 600px;
                                        margin: 50px auto;
                                        padding: 30px;
                                        background: white;
                                        border-radius: 8px;
                                        box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                                        text-align: center;
                                    }
                                    h1 {
                                        color: #522B5B;
                                        margin-bottom: 20px;
                                    }
                                    p {
                                        color: #666;
                                        line-height: 1.6;
                                        margin-bottom: 20px;
                                    }
                                    .btn {
                                        display: inline-block;
                                        padding: 10px 20px;
                                        background: #522B5B;
                                        color: white;
                                        text-decoration: none;
                                        border-radius: 4px;
                                        transition: background 0.3s;
                                    }
                                    .btn:hover {
                                        background: #854F6C;
                                    }
                                </style>
                            </head>
                            <body>
                                <div class='error-container'>
                                    <h1>Oops! Something went wrong</h1>
                                    <p>We're sorry, but we encountered an error while processing your request. Please try again or return to the dashboard.</p>
                                    <a href='/Admin' class='btn'>Return to Dashboard</a>
                                </div>
                            </body>
                        </html>");
                }
            }
        }
    }
}