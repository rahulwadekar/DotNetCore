using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var statusCodeResult = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Sorry, the resource you requested could not be found";
                    //ViewBag.Path = statusCodeResult.OriginalPath;
                    //ViewBag.QS = statusCodeResult.OriginalQueryString;
                    _logger.LogWarning($"404 error occured. The path = {statusCodeResult.OriginalPath} and Query String =  " +
              $"{statusCodeResult.OriginalQueryString}");
                    break;

                default:
                    break;
            }
            return View("NotFound");
        }

        //[Route("Error")]
        //[AllowAnonymous]
        //public IActionResult Error()
        //{

        //    var exDetails = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        //    //ViewBag.ExceptionPath = exDetails.Path;
        //    //ViewBag.ExceptionMsg = exDetails.Error.Message;
        //    //ViewBag.StackTrace = exDetails.Error.StackTrace;

        //    _logger.LogError($"The path {exDetails.Path} threw and exception " +
        //        $"{exDetails.Error}");
        //    return View("Error");
        //}

        [AllowAnonymous]
        [Route("Error")]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature =
                HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            _logger.LogError($"The path {exceptionHandlerPathFeature.Path} " +
                $"threw an exception {exceptionHandlerPathFeature.Error}");

            return View("Error");
        }
    }
}
