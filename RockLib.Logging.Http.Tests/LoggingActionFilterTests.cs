﻿using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RockLib.Logging.DependencyInjection;
using RockLib.Logging.Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.Http.Tests
{
    using static LoggingActionFilter;
    using static Logger;

    public class LoggingActionFilterTests
    {
        [Fact(DisplayName = "Constructor sets properties from non-null parameters")]
        public void ConstructorHappyPath1()
        {
            const string messageFormat = "My message format: {0}.";
            const string loggerName = "MyLogger";
            const LogLevel logLevel = LogLevel.Warn;

            var loggingActionFilter = new Mock<LoggingActionFilter>(messageFormat, loggerName, logLevel).Object;

            loggingActionFilter.MessageFormat.Should().Be(messageFormat);
            loggingActionFilter.LoggerName.Should().Be(loggerName);
            loggingActionFilter.LogLevel.Should().Be(logLevel);
        }

        [Fact(DisplayName = "Constructor sets properties from null parameters")]
        public void ConstructorHappyPath2()
        {
            var loggingActionFilter = new Mock<LoggingActionFilter>(null, null, LogLevel.Error).Object;

            loggingActionFilter.MessageFormat.Should().Be(DefaultMessageFormat);
            loggingActionFilter.LoggerName.Should().Be(DefaultName);
        }

        [Fact(DisplayName = "OnActionExecutionAsync method logs the action")]
        public async Task OnActionExecutionAsyncMethodHappyPath1()
        {
            const string messageFormat = "My message format: {0}";
            const LogLevel logLevel = LogLevel.Info;
            const string actionName = "MyAction";
            const string actionArgumentName = "foo";
            const int actionArgument = 123;

            IAsyncActionFilter loggingActionFilter = new Mock<LoggingActionFilter>(messageFormat, null, logLevel).Object;

            var mockLogger = new MockLogger();

            var httpContext = new DefaultHttpContext() { RequestServices = GetServiceProvider(mockLogger.Object) };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor() { DisplayName = actionName });
            var context = new ActionExecutingContext(actionContext, Array.Empty<IFilterMetadata>(), new Dictionary<string, object>(), null);
            context.ActionArguments.Add(actionArgumentName, actionArgument);

            var actionExecutedContext = new ActionExecutedContext(actionContext, Array.Empty<IFilterMetadata>(), null);

            ActionExecutionDelegate next = () => Task.FromResult(actionExecutedContext);

            await loggingActionFilter.OnActionExecutionAsync(context, next);

            mockLogger.VerifyInfo(string.Format(messageFormat, actionName), new { foo = 123 }, Times.Once());
        }

        [Fact(DisplayName = "OnActionExecutionAsync method sets logEntry exception from context.Exception if present")]
        public async Task OnActionExecutionAsyncMethodHappyPath2()
        {
            const string messageFormat = "My message format: {0}";
            const LogLevel logLevel = LogLevel.Info;
            const string actionName = "MyAction";
            const string actionArgumentName = "foo";
            const int actionArgument = 123;

            IAsyncActionFilter loggingActionFilter = new Mock<LoggingActionFilter>(messageFormat, null, logLevel).Object;

            var mockLogger = new MockLogger();

            var httpContext = new DefaultHttpContext() { RequestServices = GetServiceProvider(mockLogger.Object) };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor() { DisplayName = actionName });
            var context = new ActionExecutingContext(actionContext, Array.Empty<IFilterMetadata>(), new Dictionary<string, object>(), null);
            context.ActionArguments.Add(actionArgumentName, actionArgument);

            var actionExecutedContext = new ActionExecutedContext(actionContext, Array.Empty<IFilterMetadata>(), null);
            var exception = actionExecutedContext.Exception = new Exception();

            ActionExecutionDelegate next = () => Task.FromResult(actionExecutedContext);

            await loggingActionFilter.OnActionExecutionAsync(context, next);

            mockLogger.VerifyInfo(string.Format(messageFormat, actionName), new { foo = 123 }, Times.Once());
            mockLogger.Verify(m => m.Log(It.Is<LogEntry>(x => x.Exception == exception), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once());
        }

        [Fact(DisplayName = "OnActionExecutionAsync method adds 'ResultObject' extended property if context.Result is ObjectResult")]
        public async Task OnActionExecutionAsyncMethodHappyPath3()
        {
            const string messageFormat = "My message format: {0}";
            const LogLevel logLevel = LogLevel.Info;
            const string actionName = "MyAction";
            const string actionArgumentName = "foo";
            const int actionArgument = 123;
            const int resultObject = 123;

            IAsyncActionFilter loggingActionFilter = new Mock<LoggingActionFilter>(messageFormat, null, logLevel).Object;

            var mockLogger = new MockLogger();

            var httpContext = new DefaultHttpContext() { RequestServices = GetServiceProvider(mockLogger.Object) };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor() { DisplayName = actionName });
            var context = new ActionExecutingContext(actionContext, Array.Empty<IFilterMetadata>(), new Dictionary<string, object>(), null);
            context.ActionArguments.Add(actionArgumentName, actionArgument);

            var actionExecutedContext = new ActionExecutedContext(actionContext, Array.Empty<IFilterMetadata>(), null);
            actionExecutedContext.Result = new ObjectResult(resultObject);

            ActionExecutionDelegate next = () => Task.FromResult(actionExecutedContext);

            await loggingActionFilter.OnActionExecutionAsync(context, next);

            mockLogger.VerifyInfo(string.Format(messageFormat, actionName), new { foo = 123, ResultObject = resultObject }, Times.Once());
        }

        private static IServiceProvider GetServiceProvider(ILogger logger)
        {
            var services = new ServiceCollection();
            services.AddSingleton(logger);
            services.AddSingleton<LoggerLookup>(loggerName => logger);
            return services.BuildServiceProvider();
        }
    }
}
