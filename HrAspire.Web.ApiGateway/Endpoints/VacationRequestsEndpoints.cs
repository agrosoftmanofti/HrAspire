﻿namespace HrAspire.Web.ApiGateway.Endpoints;

using System.Security.Claims;

using HrAspire.Vacations.Web;
using HrAspire.Web.ApiGateway.Mappers;
using HrAspire.Web.Common;
using HrAspire.Web.Common.Models.VacationRequests;

using Microsoft.AspNetCore.Mvc;

public static class VacationRequestsEndpoints
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.ReadabilityRules",
        "SA1116:Split parameters should start on line after declaration",
        Justification = "Better readability.")]
    public static IEndpointConventionBuilder MapVacationRequestsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/").RequireAuthorization();

        group.MapGet(
            "/Employees/{employeeId}/VacationRequests",
            async (VacationRequests.VacationRequestsClient vacationRequestsClient,
                ClaimsPrincipal user,
                [FromRoute] string employeeId,
                [FromQuery] int pageNumber = 0,
                [FromQuery] int pageSize = 10) =>
            {
                var vacationRequestsResponse = await vacationRequestsClient.ListEmployeeVacationRequestsAsync(
                    new ListEmployeeVacationRequestsRequest
                    {
                        EmployeeId = employeeId,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        CurrentEmployeeId = user.GetId(),
                    });

                var vacationRequests = vacationRequestsResponse.VacationRequests.Select(e => e.MapToResponseModel()).ToList();

                return Results.Ok(new VacationRequestsResponseModel(vacationRequests, vacationRequestsResponse.Total));
            });

        group.MapPost(
            "/VacationRequests",
            async (VacationRequests.VacationRequestsClient vacationRequestsClient,
                [FromBody] VacationRequestCreateRequestModel model,
                ClaimsPrincipal user) =>
            {
                var createResponse = await vacationRequestsClient.CreateAsync(new CreateVacationRequestRequest
                {
                    EmployeeId = user.GetId()!,
                    Type = (VacationRequestType)(int)model.Type.GetValueOrDefault(),
                    FromDate = model.FromDate.GetValueOrDefault().ToTimestamp(),
                    ToDate = model.ToDate.GetValueOrDefault().ToTimestamp(),
                    Notes = model.Notes,
                });

                return Results.Created(string.Empty, createResponse.Id);
            });

        group.MapGet(
            "/VacationRequests/{id:int}",
            async (VacationRequests.VacationRequestsClient vacationRequestsClient, [FromRoute] int id, ClaimsPrincipal user) =>
            {
                var vacationRequestResponse = await vacationRequestsClient.GetAsync(
                    new GetVacationRequestRequest { Id = id, CurrentEmployeeId = user.GetId() });

                var vacationRequest = vacationRequestResponse.VacationRequest.MapToDetailsResponseModel();

                return Results.Ok(vacationRequest);
            });

        group.MapPut(
            "/VacationRequests/{id:int}",
            async (VacationRequests.VacationRequestsClient vacationRequestsClient,
                [FromRoute] int id,
                [FromBody] VacationRequestUpdateRequestModel model,
                ClaimsPrincipal user) =>
            {
                var updateResponse = await vacationRequestsClient.UpdateAsync(
                    new UpdateVacationRequestRequest
                    {
                        Id = id,
                        Type = (VacationRequestType)(int)model.Type.GetValueOrDefault(),
                        FromDate = model.FromDate.ToTimestamp(),
                        ToDate = model.ToDate.ToTimestamp(),
                        Notes = model.Notes,
                        CurrentEmployeeId = user.GetId(),
                    });

                return Results.Ok();
            });

        group.MapDelete(
            "/VacationRequests/{id:int}",
            async (VacationRequests.VacationRequestsClient vacationRequestsClient, [FromRoute] int id, ClaimsPrincipal user) =>
            {
                await vacationRequestsClient.DeleteAsync(new DeleteVacationRequestRequest { Id = id, CurrentEmployeeId = user.GetId() });

                return Results.Ok();
            });

        group
            .MapPost(
                "/VacationRequests/{id:int}/Approval",
                async (VacationRequests.VacationRequestsClient vacationRequestsClient, [FromRoute] int id, ClaimsPrincipal user) =>
                {
                    await vacationRequestsClient.ApproveAsync(
                        new ChangeStatusOfVacationRequestRequest { Id = id, CurrentEmployeeId = user.GetId()! });

                    return Results.Ok();
                })
            .RequireAuthorization(Constants.ManagerAuthPolicyName);

        group
            .MapPost(
                "/VacationRequests/{id:int}/Rejection",
                async (VacationRequests.VacationRequestsClient vacationRequestsClient, [FromRoute] int id, ClaimsPrincipal user) =>
                {
                    await vacationRequestsClient.RejectAsync(
                        new ChangeStatusOfVacationRequestRequest { Id = id, CurrentEmployeeId = user.GetId()! });

                    return Results.Ok();
                })
            .RequireAuthorization(Constants.ManagerAuthPolicyName);

        return group;
    }
}
