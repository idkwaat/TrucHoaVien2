// Helpers/ControllerExtensions.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

public static class ControllerExtensions
{
    public static int GetUserId(this ControllerBase controller)
    {
        var idClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier) ?? controller.User.FindFirst("id") ?? controller.User.FindFirst("sub");
        if (idClaim == null) throw new Exception("User id claim not found");
        return int.Parse(idClaim.Value);
    }
}
