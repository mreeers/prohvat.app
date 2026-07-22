dotnet new xunit -n ProhvatApp.Application.Tests -o src/ProhvatApp.Application.Tests
dotnet add src/ProhvatApp.Application.Tests/ProhvatApp.Application.Tests.csproj reference src/ProhvatApp.Application/ProhvatApp.Application.csproj
dotnet sln add src/ProhvatApp.Application.Tests/ProhvatApp.Application.Tests.csproj
dotnet add src/ProhvatApp.Application.Tests/ProhvatApp.Application.Tests.csproj package Moq
dotnet add src/ProhvatApp.Application.Tests/ProhvatApp.Application.Tests.csproj package FluentAssertions
