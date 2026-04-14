using CleanArchitecture.Application.Common.Mediator;
using CleanArchitecture.Application.UseCases.Products.Queries.GetAllProducts;
using CleanArchitecture.Domain.Common;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.WebAPI.Controllers;
using FluentAssertions;
using NetArchTest.Rules;
using System.Reflection;

namespace CleanArchitecture.ArchitectureTests;

public sealed class DependencyRulesTests
{
    private static readonly Assembly DomainAssembly = typeof(BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(GetAllProductsQuery).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(ApplicationDbContext).Assembly;
    private static readonly Assembly PresentationAssembly = typeof(ProductsController).Assembly;

    // ── Layer dependency rules ────────────────────────────────────────────────

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer must not depend on Application layer");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer must not depend on Infrastructure layer");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Presentation()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(PresentationAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer must not depend on Presentation layer");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application layer must not depend on Infrastructure layer");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Presentation()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(PresentationAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application layer must not depend on Presentation layer");
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Presentation()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(PresentationAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure layer must not depend on Presentation layer");
    }

    // ── Handler visibility rules ──────────────────────────────────────────────

    [Fact]
    public void UseCaseHandlers_ShouldNotBePublic()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Use Case handlers should be internal, not public");
    }

    // ── Controller isolation rules ────────────────────────────────────────────

    [Fact]
    public void Controllers_ShouldNotDirectlyDependOn_Infrastructure()
    {
        var result = Types.InAssembly(PresentationAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Controllers must go through Application (MediatR) and must not reference Infrastructure types directly");
    }

    [Fact]
    public void Controllers_ShouldNotDirectlyDependOn_Domain()
    {
        var result = Types.InAssembly(PresentationAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOn(DomainAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Controllers must communicate through Application DTOs/Commands/Queries, not Domain types");
    }

    // ── Domain entity purity rules ────────────────────────────────────────────

    [Fact]
    public void DomainEntities_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("CleanArchitecture.Domain.Entities")
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain entities must be pure business objects with no infrastructure dependencies");
    }

    [Fact]
    public void DomainEntities_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("CleanArchitecture.Domain.Entities")
            .ShouldNot()
            .HaveDependencyOn(ApplicationAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain entities must not depend on Application layer use cases or DTOs");
    }

    // ── Validator placement rules ─────────────────────────────────────────────

    [Fact]
    public void Validators_ShouldResideIn_ApplicationLayer()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .ResideInNamespaceStartingWith("CleanArchitecture.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All validators must live in the Application layer");
    }

    // ── CQRS purity rules ────────────────────────────────────────────────────

    [Fact]
    public void Commands_ShouldNotAlsoImplementIQuery()
    {
        var commandTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i == typeof(ICommand) ||
                (i.IsGenericType && (
                    i.GetGenericTypeDefinition() == typeof(ICommand<>)))))
            .ToList();

        var violations = commandTypes
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)))
            .ToList();

        violations.Should().BeEmpty(
            because: "a type must not implement both ICommand and IQuery — CQRS requires strict separation. Violations: {0}",
            string.Join(", ", violations.Select(v => v.Name)));
    }

    [Fact]
    public void Handlers_ShouldNotDependOnOtherHandlers()
    {
        var handlerSuffixes = new[] { "CommandHandler", "QueryHandler" };

        var handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => handlerSuffixes.Any(suffix => t.Name.EndsWith(suffix)))
            .ToHashSet();

        var violations = new List<string>();

        foreach (var handler in handlerTypes)
        {
            var ctors = handler.GetConstructors(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var ctor in ctors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    if (handlerTypes.Contains(param.ParameterType))
                        violations.Add($"{handler.Name} depends on {param.ParameterType.Name}");
                }
            }
        }

        violations.Should().BeEmpty(
            because: "handlers must not depend on other handlers — orchestration belongs in Application services or Sagas. Violations: {0}",
            string.Join("; ", violations));
    }
}
