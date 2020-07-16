using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.IdentityModel.Protocols;
using EO.Serwis.Portal.ServiceLayer.DTO;
using EO.Serwis.Portal.ServiceLayer;
using Serilog;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using EO.Serwis.Portal.Web.Models;
using EO.Serwis.Portal.DataAccess.Contract.POCO;

namespace EO.Serwis.Portal.Web.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class ZgloszeniaController : Controller
    {

        public PortalServiceClient Client { get; }

        public ZgloszeniaController(PortalServiceClient clientFactory)
        {
            Client = clientFactory;
        }

        public IActionResult Index(int id)
        {
            var model = Client.GetWyceny(long.Parse(User.Claims.SingleOrDefault(p => p.Type == ClaimTypes.Sid).Value));

            return View(model);
        }
    }
}