using System;

namespace TalentFlow.Domain.Exceptions;

public class BusinessConflictException : Exception
{
    public string Code { get; }

    public BusinessConflictException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
