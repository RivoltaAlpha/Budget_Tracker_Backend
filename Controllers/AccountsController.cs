using Microsoft.AspNetCore.Mvc;
using TyBudget_backend.Services;
using TyBudget_backend.Models;
using System;


namespace TyBudget_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class AccountsController : ControllerBase
    {
        private readonly IAccountsService _accountsService;

        public AccountsController(IAccountsService accountsService)
        {
            _accountsService = accountsService;
        }

        // Add account-related action methods here
    }
}