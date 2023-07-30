﻿using AutoMapper;
using RegistanFerghanaLC.DataAccess.Interfaces.Common;
using RegistanFerghanaLC.Domain.Entities.Students;
using RegistanFerghanaLC.Service.Common.Exceptions;
using RegistanFerghanaLC.Service.Common.Helpers;
using RegistanFerghanaLC.Service.Interfaces.Common;
using RegistanFerghanaLC.Service.ViewModels.StudentSubjectViewModels;
using System.Net;

namespace RegistanFerghanaLC.Service.Services.Common;

public class StudentSubjectService : IStudentSubjectService
{
    private readonly IUnitOfWork _repository;
    private readonly IMapper _mapper;

    public StudentSubjectService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        this._repository = unitOfWork;
        this._mapper = mapper;
    }
    public async Task<bool> DeleteStudentSubjectAsync(int studentSubjectId)
    {
        var studentSubject = await _repository.StudentSubjects.FindByIdAsync(studentSubjectId);
        if (studentSubject != null)
        {

            _repository.StudentSubjects.Delete(studentSubjectId);
            var res = await _repository.SaveChangesAsync();
            if (res > 0) return true;
            throw new StatusCodeException(HttpStatusCode.NotFound, "StudentSubject is not found");
        }
        else throw new StatusCodeException(HttpStatusCode.NotFound, "StudentSubject is not found");
    }

    public async Task<IEnumerable<StudentSubjectViewModel>> GetStudentSubjectAsync(int studentId)
    {
        IEnumerable<StudentSubjectViewModel> subjects = _repository.StudentSubjects.Where(x => x.StudentId == studentId).Select(x => _mapper.Map<StudentSubjectViewModel>(x));
        if (subjects != null) return subjects;
        else throw new StatusCodeException(HttpStatusCode.NotFound, "StudentSubject is not found");

    }

    public async Task<bool> SaveStudentSubjectAsync(string studentPhoneNumber, string subjectName)
    {
        var subject = await _repository.Subjects.FirstOrDefault(x => x.Name.ToLower() == subjectName.ToLower());
        var student = await _repository.Students.FirstOrDefault(x => x.PhoneNumber == studentPhoneNumber);
        if (subject != null && student != null)
        {
            StudentSubject studentSubject = new StudentSubject()
            {
                StudentId = student.Id,
                SubjectId = subject.Id,
                CreatedAt = TimeHelper.GetCurrentServerTime(),
                LastUpdatedAt = TimeHelper.GetCurrentServerTime(),
            };
            _repository.StudentSubjects.Add(studentSubject);
            var res = await _repository.SaveChangesAsync();
            if (res > 0) return true;
            else throw new StatusCodeException(HttpStatusCode.NotFound, "Student or subject is not found");
        }
        else throw new StatusCodeException(HttpStatusCode.NotFound, "Student or subject is not found");
    }


}
