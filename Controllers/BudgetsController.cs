using Microsoft.AspNetCore.Mvc;
using TyBudget_backend.Services;
using TyBudget_backend.Models.Entities;

namespace TyBudget_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class BudgetsController : ControllerBase
    {
        private readonly IBudgetsService _budgetsService;

        public BudgetsController(IBudgetsService budgetsService)
        {
            _budgetsService = budgetsService;
        }

        // Add budget-related action methods here
    }
}