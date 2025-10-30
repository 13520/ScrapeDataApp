using Microsoft.Playwright;
using Shopster.API.Model;
using System.Collections.Concurrent;
using System.Threading.Channels;
using static System.Net.WebRequestMethods;

namespace Shopster.API.Services
{
    public class ScraperService
    {
        public async Task<List<Client>> ScrapeAsync2()
        {
            var clients = new List<Client>();

            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            await page.GotoAsync("https://datatables.net/extensions/scroller/examples/initialisation/large_js_source.html");
            //https://datatables.net/extensions/scroller/examples/initialisation/state_saving.html

            // Sačekaj da se DataTable inicijalizuje
            await page.WaitForSelectorAsync("#example");

            // Pokupi sve podatke direktno iz DataTables memorije
            var data = await page.EvaluateAsync<string[][]>(@"() => {
                return $('#example').DataTable().rows().data().toArray();
            }");

            foreach (var row in data)
            {
                if (row.Length < 5) continue;

                clients.Add(new Client
                {
                    FirstName = row[1],
                    LastName = row[2],
                    ZipCode = row[3],
                    Country = row[4]
                });
            }

            return clients;
        }

        public async Task<List<Client>> ScrapeAsync()
        {
            using var playwright = await Playwright.CreateAsync();

            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--disable-gpu",
                    "--disable-dev-shm-usage",
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-extensions",
                    "--disable-background-networking",
                    "--disable-default-apps",
                    "--disable-sync",
                    "--disable-translate"
                }
            });

            var page = await browser.NewPageAsync();
            await page.SetViewportSizeAsync(800, 600);

            await page.GotoAsync("https://datatables.net/extensions/scroller/examples/initialisation/large_js_source.html");
            //  https://datatables.net/extensions/scroller/examples/initialisation/state_saving.html

            // Čekaj da se DataTable inicijalizuje u memoriji
            await page.WaitForFunctionAsync("() => $('#example').DataTable().rows().count() > 0");

            // Pokupi sve podatke direktno iz DataTables memorije
            var data = await page.EvaluateAsync<string[][]>(@"() => {
                return $('#example').DataTable().rows().data().toArray();
            }");

            // Mapiraj podatke u Client listu
            var clients = data
                .Where(row => row.Length >= 5)
                .Select(row => new Client
                {
                    FirstName = row[1],
                    LastName = row[2],
                    ZipCode = row[3],
                    Country = row[4]
                })
                .ToList();

            return clients;
        }

        public async Task<List<Client>> ScrapeWithChannelAsync(IEnumerable<string> urls, int maxDegreeOfParallelism = 4)
        {
            var channel = Channel.CreateUnbounded<string>();
            var clients = new ConcurrentBag<Client>();

            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--disable-gpu",
                    "--disable-dev-shm-usage",
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-extensions",
                    "--disable-background-networking",
                    "--disable-default-apps",
                    "--disable-sync",
                    "--disable-translate"
                }
            });

            // Pokreni potrošače
            var consumers = Enumerable.Range(0, maxDegreeOfParallelism).Select(_ => Task.Run(async () =>
            {
                await foreach (var url in channel.Reader.ReadAllAsync())
                {
                    try
                    {
                        var page = await browser.NewPageAsync();
                        await page.SetViewportSizeAsync(800, 600);
                        await page.GotoAsync(url);
                        await page.WaitForFunctionAsync("() => $('#example').DataTable().rows().count() > 0");

                        var data = await page.EvaluateAsync<string[][]>(@"() => {
                            return $('#example').DataTable().rows().data().toArray();
                        }");

                        // Mapiraj podatke u Client listu
                        foreach (var row in data.Where(r => r.Length >= 5))
                        {
                            clients.Add(new Client
                            {
                                FirstName = row[1],
                                LastName = row[2],
                                ZipCode = row[3],
                                Country = row[4]
                            });
                        }


                        await page.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        // Loguj grešku ako želiš
                    }
                }
            }));

            // Ubaci URL-ove u kanal
            foreach (var url in urls)
            {
                await channel.Writer.WriteAsync(url);
            }

            channel.Writer.Complete(); // Zatvori kanal

            await Task.WhenAll(consumers); // Sačekaj sve potrošače

            return clients.ToList();
        }

        public async Task<List<Client>> ScrapeMultipleAsync(IEnumerable<string> urls)
        {
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--disable-gpu",
                    "--disable-dev-shm-usage",
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-extensions",
                    "--disable-background-networking",
                    "--disable-default-apps",
                    "--disable-sync",
                    "--disable-translate"
                }
            });

            var tasks = urls.Select(async url =>
            {
                var page = await browser.NewPageAsync();
                await page.SetViewportSizeAsync(800, 600);
                await page.GotoAsync(url);

                await page.WaitForFunctionAsync("() => $('#example').DataTable().rows().count() > 0");

                var data = await page.EvaluateAsync<string[][]>(@"() => {
                    return $('#example').DataTable().rows().data().toArray();
                }");

                await page.CloseAsync();

                return data
                    .Where(row => row.Length >= 5)
                    .Select(row => new Client
                    {
                        FirstName = row[1],
                        LastName = row[2],
                        ZipCode = row[3],
                        Country = row[4]
                    })
                    .ToList();
            });

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(x => x).ToList();
        }


    }
}
