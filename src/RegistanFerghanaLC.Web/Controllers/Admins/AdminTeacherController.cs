﻿using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using RegistanFerghanaLC.Service.Common.Exceptions;
using RegistanFerghanaLC.Service.Common.Utils;
using RegistanFerghanaLC.Service.Dtos.FileViewModels;
using RegistanFerghanaLC.Service.Dtos.Teachers;
using RegistanFerghanaLC.Service.Interfaces.Admins;
using RegistanFerghanaLC.Service.Interfaces.Files;

namespace RegistanFerghanaLC.Web.Controllers.Admins
{
    [Route("adminteachers")]
    public class AdminTeacherController : Controller
    {
        private readonly IAdminTeacherService _adminTeacherService;
        private readonly string _rootPath;
        private readonly int _pageSize = 10;
        private readonly IExcelService _excelService;
        private readonly IAdminSubjectService _subjectService;

        public AdminTeacherController(IAdminTeacherService adminTeacherService, IWebHostEnvironment webHostEnvironment, IExcelService excelService, IAdminSubjectService adminSubjectService)
        {
            this._rootPath = webHostEnvironment.WebRootPath;
            this._adminTeacherService = adminTeacherService;
            this._excelService = excelService;
            this._subjectService = adminSubjectService;
        }

        #region GetAll
        [HttpGet]
        public async Task<IActionResult> Index(string search, int page = 1)
        {
            if (String.IsNullOrEmpty(search))
            {
                FileModeldto teachers = new FileModeldto()
                {
                    Teachers = await _adminTeacherService.GetAllAsync(new PaginationParams(page, _pageSize)),
                };
                ViewBag.HomeTitle = "Teacher";
                ViewBag.AdminTeacherSearch = search;
                return View("Index", teachers);
            }
            else
            {
                FileModeldto teachers = new FileModeldto()
                {
                    Teachers = await _adminTeacherService.SearchAsync(new PaginationParams(page, _pageSize), search)
                };
                ViewBag.HomeTitle = "Teacher";
                ViewBag.AdminTeacherSearch = search;
                return View("Index", teachers);
            }
            //return View("Index", (teachers, filemodeldto ));
        }
        #endregion

        #region Register
        [HttpGet("register")]
        public ViewResult Register()
        {
            ViewBag.Subjects = _subjectService.GetAllAsync();
            return View("Register");
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterTeacherAsync(TeacherRegisterDto teacherRegisterDto)
        {
            if (ModelState.IsValid)
            {
                var result = await _adminTeacherService.RegisterTeacherAsync(teacherRegisterDto);
                if (result)
                {
                    return RedirectToAction("index", "adminteachers", new { area = "" });
                }
                else
                {
                    return Register();
                }
            }
            else return Register();
        }
        #endregion

        #region Delete
        [HttpGet("Delete")]
        public async Task<ViewResult> DeleteAsync(int id)
        {
            var teacher = await _adminTeacherService.GetByIdAsync(id);
            if (teacher != null)
            {
                return View("Delete", teacher);
            }
            return View("adminteacher");
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteTeacherAsync(int Id)
        {
            var res = await _adminTeacherService.DeleteAsync(Id);
            if (res) return RedirectToAction("index", "adminteacher", new { area = "" });
            return View();
        }

        [HttpPost("deleteImage")]
        public async Task<IActionResult> DeleteImageAsync(int teacherId)
        {
            var image = await _adminTeacherService.DeleteImageAsync(teacherId);
            if (image) return await UpdateRedirectAsync(teacherId);
            else return await UpdateRedirectAsync(teacherId);
        }
        #endregion

        #region GetPhoneNumber
        [HttpGet("phoneNumber")]
        public async Task<IActionResult> GetByPhoneNumber(string phoneNumber)
        {
            var teacher = await _adminTeacherService.GetByPhoneNumberAsync(phoneNumber);
            ViewBag.HomeTitle = "Profile";
            var teacherView = new TeacherViewDto()
            {
                Id = teacher.Id,
                FirstName = teacher.FirstName,
                LastName = teacher.LastName,
                ImagePath = teacher.ImagePath,
                PhoneNumber = teacher.PhoneNumber,
                BirthDate = teacher.BirthDate,
                PartOfDay = teacher.PartOfDay,
                Subject = teacher.Subject,
                TeacherLevel = teacher.TeacherLevel,
                WorkDays = teacher.WorkDays,
                CreatedAt = teacher.CreatedAt
            };

            return View("Profile", teacherView);
        }
        #endregion

        #region Update
        [HttpGet("updateredirect")]
        public async Task<IActionResult> UpdateRedirectAsync(int teacherId)
        {
            var teacher = await _adminTeacherService.GetByIdAsync(teacherId);
            ViewBag.Subjects = _subjectService.GetAllAsync();
            var dto = new TeacherUpdateDto()
            {
                Id = teacher.Id,
                FirstName = teacher.FirstName,
                LastName = teacher.LastName,
                ImagePath = teacher.ImagePath!,
                WorkDays = teacher.WorkDays,
                PhoneNumber = teacher.PhoneNumber,
                TeacherLevel = teacher.TeacherLevel,
                BirthDate = teacher.BirthDate,
                Subject = teacher.Subject,
                PartOfDay = teacher.PartOfDay,
                Description = teacher.Description,
            };

            ViewBag.HomeTittle = "Admin/Teacher/Update";
            ViewBag.teacherId = teacherId;
            return View("Update", dto);
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateAsync(int teacherId, TeacherUpdateDto dto)
        {
            var res = await _adminTeacherService.UpdateAsync(dto, teacherId);
            if (res)
            {
                return await UpdateRedirectAsync(teacherId);
            }
            else return await UpdateRedirectAsync(teacherId);
        }

        [HttpPost("updatImage")]
        public async Task<IActionResult> UpdateImageAsync(int teacherId, [FromForm] IFormFile formFile)
        {
            var image = await _adminTeacherService.UpdateImageAsync(teacherId, formFile);
            return View("adminteachers");
        }
        #endregion

        #region Excel
        [HttpGet("duplicate")]
        public async Task<IActionResult> Duplicate()
        {
            using (var stream = new FileStream(Path.Combine(_rootPath, "files", "template.xlsx"), FileMode.Open))
            {
                byte[] file = new byte[stream.Length];
                await stream.ReadAsync(file, 0, file.Length);
                return new FileContentResult(file,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    FileDownloadName = $"brands_{DateTime.UtcNow.ToShortDateString()}.xlsx"
                };
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportAsync(FileModeldto filemodel)
        {
            if (filemodel.File is not null)
            {
                try
                {
                    List<TeacherRegisterDto> dtos = await _excelService.ReadTeacherFileAsync(filemodel);

                    if (dtos.Count > 0) return View("Unsaved", dtos);

                    return RedirectToAction("Index", "adminteachers", new { area = "" });
                }
                catch (InvalidExcel ex)
                {
                    return BadRequest(ex.Mes);
                }
                catch (AlreadyExistingException ex)
                {
                    return BadRequest(ex.Message);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            else
            {
                return RedirectToAction("Index", "adminteachers", new { area = "" });
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            List<TeacherViewDto> teachers = await _adminTeacherService.GetFileAllAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Brands");

                worksheet.Cell("A1").Value = "Id";
                worksheet.Cell("B1").Value = "Full Name";
                worksheet.Cell("C1").Value = "Birth Data";
                worksheet.Cell("D1").Value = "Phone Number";
                worksheet.Cell("E1").Value = "Subject";
                worksheet.Cell("F1").Value = "Teacher Level";
                worksheet.Cell("G1").Value = "Part of Day";
                worksheet.Cell("H1").Value = "Work Days";
                worksheet.Row(1).Style.Font.Bold = true;

                //нумерация строк/столбцов начинается с индекса 1 (не 0)
                for (int i = 1; i <= teachers.Count; i++)
                {
                    var teach = teachers[i - 1];
                    worksheet.Cell(i + 1, 1).Value = teach.Id;
                    worksheet.Cell(i + 1, 2).Value = teach.FirstName + " " + teach.LastName;
                    worksheet.Cell(i + 1, 3).Value = teach.BirthDate;
                    worksheet.Cell(i + 1, 4).Value = teach.PhoneNumber;
                    worksheet.Cell(i + 1, 5).Value = teach.Subject;
                    worksheet.Cell(i + 1, 6).Value = teach.TeacherLevel;
                    worksheet.Cell(i + 1, 7).Value = teach.PartOfDay;
                    if (teach.WorkDays == true) worksheet.Cell(i + 1, 8).Value = "Daytime";
                    else worksheet.Cell(i + 1, 8).Value = "Night";
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Teachers.xlsx");
                }
            }
        }
        #endregion
    }
}
