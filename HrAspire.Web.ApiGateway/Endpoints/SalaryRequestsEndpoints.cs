﻿namespace HrAspire.Web.ApiGateway.Endpoints;

using System.Security.Claims;

using HrAspire.Business.Common;
using HrAspire.Salaries.Web;
using HrAspire.Web.ApiGateway.Mappers;
using HrAspire.Web.Common;
using HrAspire.Web.Common.Models.SalaryRequests;

using Microsoft.AspNetCore.Mvc;

public static class SalaryRequestsEndpoints
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.ReadabilityRules",
        "SA1116:Split parameters should start on line after declaration",
        Justification = "Better readability.")]
    public static IEndpointConventionBuilder MapSalaryRequestsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/").RequireAuthorization();

        group
            .MapGet(
                "/SalaryRequests",
                async (SalaryRequests.SalaryRequestsClient salaryRequestsClient,
                    [FromQuery] int pageNumber = 0,
                    [FromQuery] int pageSize = 10) =>
                {
                    var salaryRequestsResponse = await salaryRequestsClient.ListAsync(
                        new ListSalaryRequestsRequest { PageNumber = pageNumber, PageSize = pageSize });

                    var salaryRequests = salaryRequestsResponse.SalaryRequests.Select(e => e.MapToResponseModel()).ToList();

                    return Results.Ok(new SalaryRequestsResponseModel(salaryRequests, salaryRequestsResponse.Total));
                })
            .RequireAuthorization(Constants.HrManagerAuthPolicyName);

        group
            .MapGet(
                "/Employees/{employeeId}/SalaryRequests",
                async (SalaryRequests.SalaryRequestsClient salaryRequestsClient,
                    ClaimsPrincipal user,
                    [FromRoute] string employeeId,
                    [FromQuery] int pageNumber = 0,
                    [FromQuery] int pageSize = 10) =>
                {
                    var managerId = user.IsInRole(BusinessConstants.ManagerRole) ? user.GetId() : null;

                    var salaryRequestsResponse = await salaryRequestsClient.ListEmployeeSalaryRequestsAsync(
                        new ListEmployeeSalaryRequestsRequest
                        {
                            EmployeeId = employeeId,
                            PageNumber = pageNumber,
                            PageSize = pageSize,
                            ManagerId = managerId,
                        });

                    var salaryRequests = salaryRequestsResponse.SalaryRequests.Select(e => e.MapToResponseModel()).ToList();

                    return Results.Ok(new SalaryRequestsResponseModel(salaryRequests, salaryRequestsResponse.Total));
                })
            .RequireAuthorization(Constants.ManagerOrHrManagerAuthPolicyName);

        group
            .MapPost(
                "/Employees/{employeeId}/SalaryRequests",
                async (SalaryRequests.SalaryRequestsClient salaryRequestsClient,
                    [FromRoute] string employeeId,
                    [FromBody] SalaryRequestCreateRequestModel model,
                    ClaimsPrincipal user) =>
                {
                    var createResponse = await salaryRequestsClient.CreateAsync(new CreateSalaryRequestRequest
                    {
                        EmployeeId = employeeId,
                        NewSalary = model.NewSalary,
                        Notes = model.Notes,
                        CreatedById = user.GetId()!,
                    });

                    return Results.Created(string.Empty, createResponse.Id);
                })
            .RequireAuthorization(Constants.ManagerAuthPolicyName);

        group
            .MapGet(
                "/SalaryRequests/{id:int}",
                async (SalaryRequests.SalaryRequestsClient salaryRequestsClient, [FromRoute] int id, ClaimsPrincipal user) =>
                {
                    var managerId = user.IsInRole(BusinessConstants.ManagerRole) ? user.GetId() : null;

                    var salaryRequestResponse = await salaryRequestsClient.GetAsync(
                        new GetSalaryRequestRequest { Id = id, ManagerId = managerId });

                    var salaryRequest = salaryRequestResponse.SalaryRequest.MapToDetailsResponseModel();

                    return Results.Ok(salaryRequest);
                })
            .RequireAuthorization(Constants.ManagerOrHrManagerAuthPolicyName);

        group
            .MapPut(
                "/SalaryRequests/{id:int}",
                async (SalaryRequests.SalaryRequestsClient salaryRequestsClient,
                    [FromRoute] int id,
                    [FromBody] SalaryRequestUpdateRequestModel model,
                    ClaimsPrincipal user) =>
                {
                    var updateResponse = await salaryRequestsClient.UpdateAsync(
                        new UpdateSalaryRequestRequest
                        {
                            Id = id,
                            NewSalary = model.NewSalary,
                            Notes = model.Notes,
                            CurrentEmployeeId = user.GetId()!,
                        });

                    return Results.Ok();
                })
            .RequireAuthorization(Constants.ManagerAuthPolicyName);

        group
            .MapDelete(
                "/SalaryRequests/{id:int}",
                async (SalaryRequests.SalaryRequestsClient salaryRequestsClient, [FromRoute] int id, ClaimsPrincipal user) =>
                {
                    await salaryRequestsClient.DeleteAsync(new DeleteSalaryRequestRequest { Id = id, CurrentEmployeeId = user.GetId()! });

                    return Results.Ok();
                })
            .RequireAuthorization(Constants.ManagerAuthPolicyName);

        group
            .MapPost(
                "/SalaryRequests/{id:int}/Approval",
                async (SalaryRequests.SalaryRequestsClient salaryRequestsClient, [FromRoute] int id, ClaimsPrincipal user) =>
                {
                    await salaryRequestsClient.ApproveAsync(
                        new ChangeStatusOfSalaryRequestRequest { Id = id, CurrentEmployeeId = user.GetId()! });

                    return Results.Ok();
                })
            .RequireAuthorization(Constants.HrManagerAuthPolicyName);

        group
            .MapPost(
                "/SalaryRequests/{id:int}/Rejection",
                async (SalaryRequests.SalaryRequestsClient salaryRequestsClient, [FromRoute] int id, ClaimsPrincipal user) =>
                {
                    await salaryRequestsClient.RejectAsync(
                        new ChangeStatusOfSalaryRequestRequest { Id = id, CurrentEmployeeId = user.GetId()! });

                    return Results.Ok();
                })
            .RequireAuthorization(Constants.HrManagerAuthPolicyName);

        return group;
    }
}
