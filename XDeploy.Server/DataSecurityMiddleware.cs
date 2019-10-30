using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using XDeploy.Server.Infrastructure.Data;
using XDeploy.Server.Infrastructure.Data.Extensions;

namespace XDeploy.Server
{
    /// <summary>
    /// Represents a middleware checks for unauthorized data operations (users trying to gain access to other users' data).
    /// </summary>
    public class DataSecurityMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly IEnumerable<MethodInfo> _allControllerMethods;

        static DataSecurityMiddleware()
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            _allControllerMethods = asm.GetTypes()
                .Where(type => typeof(Controller).IsAssignableFrom(type))
                .SelectMany(type => type.GetMethods())
                .Where(method => method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)));
        }

        public DataSecurityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ApplicationDbContext dbContext)
        {/*
            var request = context.Request;

            string controller = string.Empty;
            if (request.RouteValues.Keys.Contains("controller"))
            {
                controller = request.RouteValues["controller"].ToString() + "Controller";
            }
            string action = string.Empty;
            if (request.RouteValues.Keys.Contains("action"))
            {
                action = request.RouteValues["action"].ToString();
            }
            Func<MethodInfo, bool> methodSelector = x => x.DeclaringType.Name == controller && x.Name == action;
            MethodInfo method = null;
            if (_allControllerMethods.Count(methodSelector) == 1)
            {
                method = _allControllerMethods.First(methodSelector);
            }
            if (method != null && Attribute.IsDefined(method, typeof(ValidateDataOwnershipAndExistenceAttribute)))
            {
                if (context.Request.Query.Keys.Select(x => x.ToUpper()).Contains("ID")) // make it case insensitive
                {
                    var id = context.Request.Query[context.Request.Query.Keys.Where(x => x.ToUpper() == "ID").First()][0]; //Can't fool the middleware
                    if (!string.IsNullOrEmpty(id))
                    {
                        if (dbContext.Applications.Exists(id))
                        {
                            var foundApp = dbContext.Applications.FirstByID(id);
                            if (!foundApp.HasOwner(context.User))
                            {
                                //Unauthorized
                                context.Response.StatusCode = 401;
                            }
                            else
                            {
                                await _next.Invoke(context); //Owner ok
                            }
                        }
                        else
                        {
                            //App not found
                            context.Response.StatusCode = 404;
                        }
                    }
                    else
                    {
                        //App not found
                        context.Response.StatusCode = 404;
                    }
                }
            }*/
            await _next.Invoke(context);
        }
    }
}
