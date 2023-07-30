using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RegistanFerghanaLC.DataAccess.Interfaces.Common;
using RegistanFerghanaLC.Service.Common.Exceptions;
using RegistanFerghanaLC.Service.Common.Security;
using RegistanFerghanaLC.Service.Common.Utils;
using RegistanFerghanaLC.Service.Dtos.Accounts;
using RegistanFerghanaLC.Service.Interfaces.Common;
using RegistanFerghanaLC.Service.Interfaces.Students;
using RegistanFerghanaLC.Service.ViewModels.StudentViewModels;
using RegistanFerghanaLC.Service.ViewModels.TeacherViewModels;
using System.Net;

namespace RegistanFerghanaLC.Service.Services.StudentService;
public class StudentService : IStudentService
{
    private readonly IUnitOfWork _repository;
    private readonly IAuthService _authService;
    private readonly IImageService _imageService;
    private readonly IMapper _mapper;
    private readonly IIdentityService _identityService;

    public StudentService(IUnitOfWork unitOfWork, IAuthService authService, IImageService imageService, IMapper mapper, IIdentityService identityService)
    {
        this._repository = unitOfWork;
        this._authService = authService;
        this._imageService = imageService;
        this._mapper = mapper;
        this._identityService = identityService;
    }

    public Task<PagedList<TeacherBySubjectViewModel>> GetAllTeacherBySubjectAsync(string subject, PaginationParams @params)
    {
        var query = from teacher in _repository.Teachers.GetAll().Where(x => x.Subject.ToLower() == subject.ToLower())
                    select new TeacherBySubjectViewModel()
                    {
                        Id = teacher.Id,
                        FirstName = teacher.FirstName,
                        LastName = teacher.LastName,
                        Description = teacher.Description,
                        TeacherLevel = teacher.TeacherLevel,
                        WorkDays = teacher.WorkDays,
                        ImagePath = teacher.Image
                    };
        return PagedList<TeacherBySubjectViewModel>.ToPagedListAsync(query, @params);
    }

    public async Task<bool> ImageUpdateAsync(int id, IFormFile path)
    {
        var student = await _repository.Students.FindByIdAsync(id);
        if (student == null)
            throw new StatusCodeException(System.Net.HttpStatusCode.NotFound, "teacher is not found");
        _repository.Students.TrackingDeteched(student);
        if (student.Image != null)
        {
            await _imageService.DeleteImageAsync(student.Image);
        }
        student.Image = await _imageService.SaveImageAsync(path);
        _repository.Students.Update(id, student);
        int res = await _repository.SaveChangesAsync();
        return res > 0;
    }

    public Task<int> GetLimitStudentAsync(int id)
    {
        DateTime date;
        var day = DateTime.Now.DayOfWeek;
        if (day == DayOfWeek.Friday) date = DateTime.Now.Date.AddDays(-4);
        else if (day == DayOfWeek.Monday) date = DateTime.Now.Date;
        else if (day == DayOfWeek.Tuesday) date = DateTime.Now.Date.AddDays(-1);
        else if (day == DayOfWeek.Wednesday) date = DateTime.Now.Date.AddDays(-2);
        else if (day == DayOfWeek.Thursday) date = DateTime.Now.Date.AddDays(-3);
        else if (day == DayOfWeek.Saturday) date = DateTime.Now.Date.AddDays(-5);
        else date = DateTime.Now.Date.AddDays(-6);

        var limit = _repository.ExtraLessons.GetAll().Where(x => x.CreatedAt > date).CountAsync();

        return limit;
    }

    public async Task<bool> DeleteImageAsync(int id)
    {
        var student = await _repository.Students.FindByIdAsync(id);
        await _imageService.DeleteImageAsync(student.Image);
        student.Image = "";
        _repository.Students.Update(id, student);
        var result = await _repository.SaveChangesAsync();
        return result > 0;
    }

    public async Task<string> LoginAsync(AccountLoginDto accountLoginDto)
    {
        var student = await _repository.Students.FirstOrDefault(x => x.PhoneNumber == accountLoginDto.PhoneNumber);
        if (student is null)
            throw new StatusCodeException(HttpStatusCode.NotFound, "Student not found");
        else
        {
            var passwordHasher = PasswordHasher.Verify(accountLoginDto.Password, student.Salt, student.PasswordHash);
            if (passwordHasher)
            {
                string token = _authService.GenerateToken(student, "Student");
                return token;
            }
            else throw new StatusCodeException(HttpStatusCode.NotFound, "Incorrect Password");
        }
    }

    public async Task<StudentViewModel> GetByIdAsync(int id)
    {
        var student = await _repository.Students.FindByIdAsync(id);
        if (student is null) throw new StatusCodeException(HttpStatusCode.NotFound, "Student not found!");
        var res = _mapper.Map<StudentViewModel>(student);
        return res;
    }

    public async Task<StudentViewModel> GetByTokenAsync()
    {
        var student = await _repository.Students.FindByIdAsync(int.Parse(_identityService.Id!.Value.ToString()));
        if (student is null) throw new StatusCodeException(HttpStatusCode.NotFound, "Student not found!");
        var result = _mapper.Map<StudentViewModel>(student);
        return result;
    }
}