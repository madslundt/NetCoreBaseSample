﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Filters
{
    public class ExceptionFilter : Attribute, IExceptionFilter
    {
        private HttpStatusCode MapStatusCode(Exception ex)
        {
            return ex switch
            {
                ArgumentException _ => HttpStatusCode.NotFound,
                ValidationException _ => HttpStatusCode.BadRequest,
                UnauthorizedAccessException _ => HttpStatusCode.Unauthorized,
                NotSupportedException _ => HttpStatusCode.MethodNotAllowed,
                DuplicateNameException _ => HttpStatusCode.Conflict,
                _ => HttpStatusCode.InternalServerError
            };
        }

        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ExceptionFilter> _logger;

        public ExceptionFilter(
            IWebHostEnvironment env,
            ILogger<ExceptionFilter> logger)
        {
            _env = env;
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception != null)
            {
                var content = new Dictionary<string, object>
                {
                    {"ErrorMessage", context.Exception.Message}
                };

                if (_env.IsDevelopment())
                {
                    content.Add("Exception", context.Exception);
                }

                var statusCode = (int) MapStatusCode(context.Exception);

                LogError(context, statusCode);

                context.Result = new ObjectResult(content);
                context.HttpContext.Response.StatusCode = statusCode;
                context.Exception = null;
            }
        }

        private void LogError(ExceptionContext context, int statusCode)
        {
            var logTitle = $"{context.HttpContext.Request.Path} :: [{statusCode}] {context.Exception.Message}";
            var logError = new
            {
                Context = context,
            };

            if (statusCode >= 500)
            {
                _logger.LogCritical(logTitle, logError);
            }
            else if (statusCode == 404 || statusCode == 401)
            {
                _logger.LogInformation(logTitle, logError);
            }
            else
            {
                _logger.LogWarning(logTitle, logError);
            }
        }
    }
}