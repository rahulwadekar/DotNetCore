using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmployeeManagement.Models;
using EmployeeManagement.Security;
using EmployeeManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Controllers
{
    //[Route("Home")]
    //[Route("[controller]")] //can use tokens such as controller, action instead of actual names
    //[Route("[controller]/[action]")] //can use tokens such as controller, action instead of actual names

    //public class WelcomeController : Controller //Controller name and action method  names adoes not matter while specifying attribute routing
    public class HomeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger _logger;
        private readonly IDataProtector protector;
        //public /*IActionResult*/ string Index()
        //{
        //    return "Hello World form Controller";//View();
        //}
        public HomeController(IEmployeeRepository employeeRepository,
            IHostingEnvironment hostingEnvironment,
            ILogger<HomeController> logger,
            IDataProtectionProvider dataProtectionProvider,
            DataProtectionPurposeStrings dataProtectionPurposeStrings)
        {
            _employeeRepository = employeeRepository;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;

            // Pass the purpose string as a parameter
            this.protector = dataProtectionProvider.CreateProtector(
                dataProtectionPurposeStrings.EmployeeIdRouteValue);

        }

        //public JsonResult Index()
        //{
        //    return Json(new {id=1, name="Rahul"});
        //}


        //[Route("/")]
        //[Route("")]  // default
        //[Route("/Home")]
        //[Route("/Home/Index")]
        //[Route("Index")] // Home is mentioned in controller route
        //[Route("[action]")] //can use tokens such as controller, action instead of actual names
        [AllowAnonymous]
        public ViewResult Index()
        {
            var model = _employeeRepository.GetAllEmployee()
                            .Select(e =>
                            {
                                // Encrypt the ID value and store in EncryptedId property
                                e.EncryptedId = protector.Protect(e.Id.ToString());
                                return e;
                            });
            return View(model);
        }

        //public JsonResult Details2()
        //{
        //    Employee employee = _employeeRepository.GetEmployee(1);

        //    return Json(employee);
        //}

        //public ObjectResult Details1()
        //{
        //    Employee employee = _employeeRepository.GetEmployee(1);
        //    return new ObjectResult(employee);
        //}

        //public ViewResult Details()
        //{
        //    Employee employee = _employeeRepository.GetEmployee(1);

        //    //View Data
        //    //ViewData["Employee"]=employee;
        //    //ViewData["PageTitle"]= "Employee Details";

        //    //View Bag - uses dynamic properties
        //    //ViewBag.Employee = employee;
        //    //ViewBag.PageTitle = "Employee Details";

        //    HomeDetailsViewModels hdvm = new HomeDetailsViewModels();
        //    hdvm.employee = employee;
        //    hdvm.PageTitle = "Employee Details";
        //    //ViewBag and ViewData - no compile time type checking 
        //    //return View(employee);
        //    return View(hdvm);
        //}

        //[Route("[action]/{id?}")] //can use tokens such as controller, action instead of actual names
        //[Route("Details/{id?}")]  // Home is mentioned in controller route
        //[Route("Home/Details/{id?}")]
        //public ViewResult Details(int? id)
        //{
        //    //throw new Exception("Error in details page");
        //    _logger.LogTrace("Trace Log");
        //    _logger.LogDebug("Debug Log");
        //    _logger.LogInformation("Information Log");
        //    _logger.LogWarning("Warning Log");
        //    _logger.LogError("Error Log");
        //    _logger.LogCritical("Critical Log");

        //    Employee employee = _employeeRepository.GetEmployee(id.Value);

        //    if (employee == null)
        //    {
        //        Response.StatusCode = 404;
        //        return View("EmployeeNotFound", id.Value);
        //    }

        //    HomeDetailsViewModels hdvm = new HomeDetailsViewModels();
        //    hdvm.employee = employee;
        //    hdvm.PageTitle = "Employee Details";

        //    return View(hdvm);
        //}

        // Details view receives the encrypted employee ID
        [AllowAnonymous]
        public ViewResult Details(string id)
        {
            // Decrypt the employee id using Unprotect method
            string decryptedId = protector.Unprotect(id);
            int decryptedIntId = Convert.ToInt32(decryptedId);

            Employee employee = _employeeRepository.GetEmployee(decryptedIntId);

            HomeDetailsViewModels hdvm = new HomeDetailsViewModels();
            hdvm.employee = employee;
            hdvm.PageTitle = "Employee Details";

            return View(hdvm);
        }

        [HttpGet]
        [Authorize]
        public ViewResult Create(int? id)
        {
            Employee employee = _employeeRepository.GetEmployee(id ?? 1);
            EmployeeCreateViewModel evm = new EmployeeCreateViewModel();
            return View(evm);
        }


        [HttpPost]
        [Authorize]
        public IActionResult Create(EmployeeCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                string uniqueFileName = ProcessUploadedFile(model);
                //Employee newemployee = _employeeRepository.Add(employee);
                Employee newemployee = new Employee
                {
                    Name = model.Name,
                    Email = model.Email,
                    Department = model.Department,
                    PhotoPath = uniqueFileName
                };
                _employeeRepository.Add(newemployee);
                return RedirectToAction("Details", new { id = newemployee.Id });
            }

            return View();
        }


        [HttpGet]
        [Authorize]
        public ViewResult Edit(int? id)
        {
            Employee employee = _employeeRepository.GetEmployee(id ?? 1);
            EmployeeEditViewModel employeeEditViewModel = new EmployeeEditViewModel
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Department = employee.Department,
                ExistingPhotoPath = employee.PhotoPath
            };

            return View(employeeEditViewModel);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Edit(EmployeeEditViewModel model)
        {
            if (ModelState.IsValid)
            {

                Employee employee = _employeeRepository.GetEmployee(model.Id);

                employee.Name = model.Name;
                employee.Email = model.Email;
                employee.Department = model.Department;
                if (model.Photos != null)
                {
                    if (model.ExistingPhotoPath != null)
                    {
                        string filePath = Path.Combine(_hostingEnvironment.WebRootPath,
                             "ïmages", model.ExistingPhotoPath);
                        System.IO.File.Delete(filePath);
                    }
                    employee.PhotoPath = ProcessUploadedFile(model);
                }

                _employeeRepository.Update(employee);
                return RedirectToAction("index", new { id = employee.Id });
            }

            return View();
        }

        private string ProcessUploadedFile(EmployeeCreateViewModel model)
        {
            string uniqueFileName = null;
            if (model.Photos != null && model.Photos.Count > 0)
            {
                foreach (IFormFile photo in model.Photos)
                {
                    string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images");
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        photo.CopyTo(fileStream);
                    }
                }
            }

            return uniqueFileName;
        }

        //Specifying View explicitly
        //public ViewResult Details()
        //{
        //    Employee employee = _employeeRepository.GetEmployee(1);
        //    return View("Test", employee);
        //}

        //Specifying View explicitly
        //public ViewResult Details()
        //{
        //    Employee employee = _employeeRepository.GetEmployee(1);
        //    return View("MYViews/Test", employee);
        //    return View("/MYViews/Test", employee);
        //    return View("~/MYViews/Test", employee);
        //    return View("../Test/Update", employee);
        //    return View("../../MyViews/Test", employee);
        //}


    }
}
