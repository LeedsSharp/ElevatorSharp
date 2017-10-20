using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using ElevatorSharp.Compilers;
using ElevatorSharp.Domain;
using ElevatorSharp.Domain.DataTransferObjects;
using ElevatorSharp.Game;
using ElevatorSharp.Game.Players;
using ElevatorSharp.Tests.Players;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;

namespace ElevatorSharp.Web.Controllers
{
    public class SkyscraperController : Controller
    {
        public ActionResult Index(string message = null, string source = null, string diagnostics = null, string language = null)
        {
            ViewBag.Message = message;
            ViewBag.Source = string.IsNullOrWhiteSpace(source) || !MemoryCache.Default.Contains(source) 
                ? _defaultCodeCSharp 
                : (string)MemoryCache.Default.Get(source);
	        ViewBag.CSharpSource = _defaultCodeCSharp;
			ViewBag.FSharpSource = _defaultCodeFSharp;
	        ViewBag.Language = string.IsNullOrWhiteSpace(language)
		        ? "csharp"
		        : language;

			if (diagnostics != null && MemoryCache.Default.Contains(diagnostics))
            {
                ViewBag.Diagnostics = (IEnumerable<string>)MemoryCache.Default.Get(diagnostics);
            }
            else
            {
                ViewBag.Diagnostics = new string[0];
            }
            return View();
        }

	    private string _defaultCodeFSharp = @"namespace ElevatorSharp.Default

open System
open ElevatorSharp.Game
open ElevatorSharp.Game.Players

type TestPlayer() =
    interface IPlayer with
        member this.Init (elevators, floors) =
            let elevator = elevators.[0]
            elevator.Idle.Add(fun _ -> elevator.GoToFloor 0
                                       elevator.GoToFloor 1)";

        private string _defaultCodeCSharp = @"using System;
using ElevatorSharp.Game;
using ElevatorSharp.Game.Players;

namespace ElevatorSharp.Default
{
    public class TestPlayer : IPlayer
    {
        public void Init(IElevator[] elevators, IFloor[] floors)
        {
            // Let's use the first elevator
            var elevator = elevators[0];

            // Whenever the elevator is idle (has no more queued destinations) ...
            elevator.Idle += ElevatorOnIdle;
        }
        
        private void ElevatorOnIdle(object sender, EventArgs eventArgs)
        {
            var elevator = (IElevator) sender;

            // let's go to all the floors (or did we forget one?)
            elevator.GoToFloor(0);
            elevator.GoToFloor(1);
        }
    }
}";

        public ActionResult UploadPlayer(HttpPostedFileBase dll)
        {
            var message = "Could not load assembly";
            if (dll == null || dll.ContentLength <= 0) return RedirectToAction("Index", new {message});

            // Load player dll
            var fileName = $"{Guid.NewGuid()},{Path.GetFileName(dll.FileName)}";
            var path = Path.Combine(Server.MapPath("~/App_Data"), fileName);
            dll.SaveAs(path);
            var playerAssembly = Assembly.LoadFile(path);
            return FindPlayer(playerAssembly);
        }

        private ActionResult FindPlayer(Assembly playerAssembly, string source = null, string language = null)
        {
            string message;
            foreach (var type in playerAssembly.GetTypes().OrderBy(t => t.Name))
            {
                if (type.GetInterface("IPlayer") != null)
                {
                    var player = Activator.CreateInstance(type) as IPlayer;
                    SavePlayer(player);
                    message = type.Name + " uploaded.";
                    return RedirectToAction("Index", new {message, source, language });
                }
            }
            return RedirectToAction("Index", new {message= "No player implementing IPlayer found.", language });
        }

        [ValidateInput(false)]
        public ActionResult UploadPlayerAsCode(string source, string language)
        {
	        var compiler = GetCompiler(language);
			var result = compiler.Compile(source);

			var sourceGuid = Guid.NewGuid().ToString();
			MemoryCache.Default.Add(sourceGuid, source, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddDays(1) });
			if (result.Success)
				return FindPlayer(result.Assembly, sourceGuid, language);

			var diagnosticsGuid = Guid.NewGuid().ToString();
			MemoryCache.Default.Add(diagnosticsGuid, result.Diagnostics, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddDays(1) });
			return RedirectToAction("Index", new { message = "Compilation issues", source = sourceGuid, diagnostics = diagnosticsGuid, language });
		}

		public ContentResult New(SkyscraperDto skyscraperDto)
        {
            var skyscraper = new Skyscraper(skyscraperDto);
            var player = LoadPlayer();
            skyscraper.LoadPlayer(player); // This calls the Init method on Player and hooks up events
            SaveSkyscraper(skyscraper);

            var json = JsonConvert.SerializeObject("Ready."); // TODO: Not needed, unless you want to pass some UI message back.
            return Content(json, "application/json");
        }

        #region Helper Methods
        private static void SaveSkyscraper(Skyscraper skyscraper)
        {
            var cache = MemoryCache.Default;
            cache.Remove("skyscraper");
            cache.Set("skyscraper", skyscraper, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddDays(1) });
        }

        private static void SavePlayer(IPlayer player)
        {
            var cache = MemoryCache.Default;
            cache.Set("player", player, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddDays(1) });
        }

        private static IPlayer LoadPlayer()
        {
            var cache = MemoryCache.Default;
            if (cache.Contains("player"))
            {
                return (IPlayer)cache.Get("player");
            }
            return new DevPlayer();
        }

	    private static PlayerCompiler GetCompiler(string language)
	    {
		    switch (language)
		    {
				case "csharp":
					return PlayerCompiler.CSharp;

				case "fsharp":
					return PlayerCompiler.FSharp;

				default:
					throw new ArgumentOutOfRangeException("language");
		    }
	    }
        #endregion  
    }
}