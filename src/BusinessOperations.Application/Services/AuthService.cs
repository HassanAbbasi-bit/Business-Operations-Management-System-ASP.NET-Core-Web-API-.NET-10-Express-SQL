using BusinessOperations.Application.DTOs.Auth;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Domain.Exceptions;
using FluentValidation;

namespace BusinessOperations.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<LoginRequestDto> _loginValidator;

    public AuthService(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<LoginRequestDto> loginValidator)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors[0].ErrorMessage;
            throw new DomainException(firstError);
        }

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new DomainException($"A user with email '{request.Email}' already exists.");
        }

        string passwordHash = _passwordHasher.HashPassword(request.Password);
        var role = request.Role ?? UserRole.Employee;
        var user = new User(normalizedEmail, passwordHash, role);
        await _userRepository.AddAsync(user, cancellationToken);

        Employee? employee = null;
        if (request.DepartmentId.HasValue && request.DepartmentId.Value != Guid.Empty)
        {
            employee = new Employee(
                request.FirstName,
                request.LastName,
                normalizedEmail,
                request.DepartmentId.Value,
                DateOnly.FromDateTime(DateTime.UtcNow),
                userId: user.Id);

            await _employeeRepository.AddAsync(employee, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user, employee);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            EmployeeId = employee?.Id,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors[0].ErrorMessage;
            throw new DomainException(firstError);
        }

        string rawIdentifier = !string.IsNullOrWhiteSpace(request.Email)
            ? request.Email.Trim()
            : (request.UsernameOrEmail?.Trim() ?? string.Empty);

        string normalizedEmail = rawIdentifier.ToLowerInvariant() switch
        {
            "admin" => "admin@operix.com",
            "manager" => "manager@operix.com",
            "employee" => "employee@operix.com",
            _ => rawIdentifier.ToLowerInvariant()
        };

        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new DomainException("Invalid email or password.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new DomainException("Invalid email or password.");
        }

        var employee = await _employeeRepository.GetByUserIdAsync(user.Id, cancellationToken);
        var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user, employee);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            EmployeeId = employee?.Id,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("User was not found.");

        var employee = await _employeeRepository.GetByUserIdAsync(userId, cancellationToken);

        return new CurrentUserDto
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            EmployeeId = employee?.Id,
            FullName = employee?.FullName,
            DepartmentName = employee?.Department?.Name
        };
    }
}
