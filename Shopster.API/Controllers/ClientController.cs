using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopster.API.Model;
using Shopster.API.Services;
using EFCore.BulkExtensions;

namespace Shopster.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        public readonly AppDBContext _context;
        public readonly ScraperService _scraperService;

        public ClientController(AppDBContext context, ScraperService scraperService)
        {
            _context = context;
            _scraperService = scraperService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Client>>> Get()
        {
            return await _context.Clients.ToListAsync();
        }

        [HttpPost("scrape")]
        public async Task<IActionResult> Post()
        {
            var clients = await _scraperService.ScrapeAsync();

            // Isključi EF tracking za performanse
            _context.ChangeTracker.AutoDetectChangesEnabled = false;

            int batchSize = 1000;
            for (int i = 0; i < clients.Count; i += batchSize)
            {
                var batch = clients.Skip(i).Take(batchSize).ToList();
                _context.Clients.AddRange(batch);
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear(); // očisti EF keš
            }

            // Vrati broj ubačenih redova
            return Ok(new { Count = clients.Count });
        }

        [HttpPost("scrape2")]
        public async Task<IActionResult> Post2()
        {
            var clients = await _scraperService.ScrapeAsync();

            await _context.BulkInsertAsync(clients);

            return Ok(new { Count = clients.Count });
        }

        [HttpPost("scrape-channel")]
        public async Task<IActionResult> ScrapeWithChannel()
        {
            var urls = new[]
            {
                "https://datatables.net/extensions/scroller/examples/initialisation/large_js_source.html",
                "https://datatables.net/extensions/scroller/examples/initialisation/state_saving.html"
            };

            var clients = await _scraperService.ScrapeWithChannelAsync(urls);

            await _context.BulkInsertAsync(clients);

            return Ok(new { Count = clients.Count });
        }

        [HttpPost("scrape-multiple")]
        public async Task<IActionResult> ScrapeMultipleAsync()
        {
                var urls = new[]
                {
                    "https://datatables.net/extensions/scroller/examples/initialisation/large_js_source.html",
                    "https://datatables.net/extensions/scroller/examples/initialisation/state_saving.html"
                };

            var clients = await _scraperService.ScrapeMultipleAsync(urls);

            await _context.BulkInsertAsync(clients);

            return Ok(new { Count = clients.Count });
        }


    }
}
