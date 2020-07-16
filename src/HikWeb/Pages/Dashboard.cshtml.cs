﻿using System.Collections.Generic;
using System.Threading.Tasks;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HikWeb.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly DataContext dataContext;

        public IReadOnlyCollection<Camera> Cameras { get; private set; }

        public DashboardModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task OnGet()
        {
            Cameras = await this.dataContext.Cameras.ToListAsync();
        }
    }
}