using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Claims;
using System.Threading.Tasks;
using EO.Serwis.Portal.DataAccess.Contract.POCO;
using EO.Serwis.Portal.ServiceLayer;
using EO.Serwis.Portal.ServiceLayer.DTO;
using EO.Serwis.Portal.Web.ActionFilters;
using EO.Serwis.Portal.Web.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;

namespace EO.Serwis.Portal.Web.Controllers
{
    public class PaymentsController : Controller
    {
        public PortalServiceClient Client { get; }
        public IConfiguration Conf { get; }

        public PaymentsController(PortalServiceClient client, IConfiguration conf)
        {
            Client = client;
            Conf = conf;
        }

        //public class Data
        //{
        //    public string nip { get; set; }
        //}
        //[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        //[HttpPost]
        //[ServiceFilter(typeof(ValuationAuthorizationFilter))]
        //public IActionResult Nip(Data data) 
        //{
        //    var aaa = data.nip;
            
        //    return Json(aaa);
        //}
        
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpGet("{id}")]
        [ServiceFilter(typeof(ValuationAuthorizationFilter))]
        public IActionResult Index(string id)
        {
            try
            {
                Log.Information($"Payments for id={id}");
                var userId = User.Claims.Single(p => p.Type == ClaimTypes.Sid).Value;
                var model = Client.GetWycena(id, userId);
                Log.Debug($"Wycena found: {JsonConvert.SerializeObject(model)}");

                return View(new PaymentsModel()
                {
                    UserId = userId,
                    IdWyceny = model.IdWyceny,
                    Adres = model.Ulica,
                    Cena = model.PozycjeWyceny == null ? model.CenaBrutto.ToString("C") : model.PozycjeWyceny.Sum(p => p.SumaBrutto).Value.ToString("C"),
                    City = model.Miasto,
                    Email = model.Email,
                    Lastname = model.Nazwisko,
                    IdZgloszenia = model.IdZgloszenia,
                    Model = model.Model,
                    Name = model.Imie,
                    NIP = model.NIP,
                    OpisUsterki = model.Opis,
                    PostCode = model.Kod,
                    Producent = model.Producent,
                    SerialNo = model.NumerSeryjny,
                    UrlWyceny = id,
                    DataRejestracji = model.DataRejestracji,
                    Pracownik = model.Pracownik,
                    FakturaMiasto = model.MiastoFirmy,
                    FakturaNip = model.NIP,
                    FakturaUlica = model.AdresFirmy,
                    FakturaKodPocztowy = model.KodPocztowyFirmy,
                    FakturaNazwa = model.NazwaFirmy,
                    PozycjeWyceny = model.PozycjeWyceny == null ? new List<PozycjaWycenyPOCO>() : model.PozycjeWyceny,
                    CanEdit = model.CanEdit,
                    Platnosc = model.Platnosc,
                    FakturaVat = string.IsNullOrWhiteSpace(model.NIP) == false,
                    StatusWyceny = model.StatusWyceny
                });
            }
            catch (Exception ex)
            {
                Log.Fatal(ex.ToString());
                throw ex;
            }
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpPost]
        [ActionName("Cancellation")]
        public IActionResult Cancellation(PaymentsModel model)
        {
            var userId = User.Claims.Single(p => p.Type == ClaimTypes.Sid).Value;
            Client.RejectWycena(model.UrlWyceny, userId);
            Log.Information($"Kljent dokonanał rezygnacji ze zgłoszenia: {model.IdZgloszenia}");
            return View();
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpPost]
        [ActionName("Confirmation")]
        public IActionResult Confirmation(PaymentsModel model)
        {
            var userId = User.Claims.Single(p => p.Type == ClaimTypes.Sid).Value;
            Client.AcceptWycena(model.UrlWyceny, userId);
            Log.Information($"Kljent dokonanał akceptacji zgłoszenia: {model.IdZgloszenia}");
            return View();
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpPost]
        [ActionName("Register")]
        public IActionResult Register(PaymentsModel model, ZgloszeniaDTO dTO)
        {
            try
            {
                var baseP24Url = Conf.GetSection("UserSecrets")["P24_Url"];
                Log.Debug($"Wycena posted: {JsonConvert.SerializeObject(model)}");
                WycenaDTO oldModel = null;

                RegulaminDTO regulamin = new RegulaminDTO();
                regulamin = Client.GetRegulamin();

                long idUzytkownikaPortalu = 0;
                Int64.TryParse(model.UserId, out idUzytkownikaPortalu);

                oldModel = Client.GetWycena(model.UrlWyceny, model.UserId);

                dTO = Client.GetCustomerData(model.IdZgloszenia);

                //if (dTO.WyborRealizatora != "Diagnostyka")
                //{
                 //return RedirectToAction("Confirmation", "Payments");
                //}
                //else if (dTO.WyborRealizatora == "Diagnostyka")
                //{
                    var token = Client.RegisterTransaction(new ServiceLayer.DTO.RegisterPaymentDTO()
                    {
                        IdWyceny = model.IdWyceny,
                        Adres = oldModel.Ulica,
                        AkceptacjaCenyZaWycene = model.AkceptacjaCenyZaWycene,
                        AkceptacjaRegulaminu = model.AkceptacjaRegulaminu,
                        Cena = oldModel.CenaBrutto,
                        City = oldModel.Miasto,
                        Email = oldModel.Email,
                        FakturaKodPocztowy = model.FakturaKodPocztowy,
                        FakturaMiasto = model.FakturaMiasto,
                        FakturaNazwa = model.FakturaNazwa,
                        FakturaNip = model.FakturaNip,
                        FakturaNrBudynku = model.FakturaNrBudynku,
                        FakturaNrLokalu = model.FakturaNrLokalu,
                        FakturaUlica = model.FakturaUlica,
                        FakturaVat = model.FakturaVat,
                        Lastname = oldModel.Nazwisko,
                        FirstName = oldModel.Imie,
                        Model = oldModel.Model,
                        Name = oldModel.Imie,
                        NIP = oldModel.NIP,
                        OpisUsterki = oldModel.Opis,
                        PostCode = oldModel.Kod,
                        Producent = oldModel.Producent,
                        SerialNo = oldModel.NumerSeryjny,
                        KodCRC = regulamin.KodCRC,
                        DataAkceptacji = DateTime.Now,
                        IdRegulaminuSerwisu = regulamin.Id,
                        IdUzytkownikaPortalu = idUzytkownikaPortalu,
                        IdZgloszenia = model.IdZgloszenia
                    });
                    var url = $"{baseP24Url}/trnRequest/{token}";
                Log.Debug($"Wygenerowany url do P24:{url}");
                    return Redirect(url);
                //}
                //return Ok();
            }
            catch(Exception ex)
            {
                Log.Fatal(ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpGet]
        [ActionName("FakturaZal")]
        public FileStreamResult FakturaZal(string id)
        {
            var userId = User.Claims.Single(p => p.Type == ClaimTypes.Sid).Value;
            var model = Client.GetWycena(id.ToString(), userId);
            if (model != null)
            {
                return new FileStreamResult(Client.GetInvoice(model.IdWyceny), "application/pdf");
            }
            throw new SecurityException("Brak uprawnień do dokumentu");
        }
    }
}