using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;
using System.Diagnostics;
using System.Drawing;
using OpenQA.Selenium.Support.Extensions;
using static System.Net.WebRequestMethods;


namespace Microsoft.VisualStudio.TestPlatform
{
    public class ScreenshotTests
    {
        private IWebDriver? driver; // Объявлено как nullable
        private readonly string screenshotDirectory = "C:\\Screenshots\\"; // Укажите путь к папке для сохранения скриншотов

        // Массив браузеров
        private readonly string[] browsers = ["chrome", "firefox", "edge"];

        [SetUp]
        public void Setup()
        {
            // Создание папки для скриншотов, если она не существует
            if (!Directory.Exists(screenshotDirectory))
            {
                Directory.CreateDirectory(screenshotDirectory);
            }
        }

        [Test]
        public void TakeScreenshotsTest()
        {
            var urls = new List<string>
            {
                "https://stackoverflow.com/",
                // Добавьте остальные URL-адреса страниц
            };

            var resolutions = new List<(int width, int height)>
            {
                (1920, 1080),
                (1280, 1024),
                (992, 1024),
                (768, 1024),
                (320, 480)
            };

            foreach (var browser in browsers)
            {
                // Инициализация драйвера для текущего браузера
                driver = browser.ToLower() switch
                {
                    "chrome" => new ChromeDriver(),
                    "firefox" => new FirefoxDriver(),
                    "edge" => new EdgeDriver(),
                    _ => throw new ArgumentException("Unsupported browser")
                };

                foreach (var url in urls)
                {
                    foreach (var resolution in resolutions)
                    {
                        driver.Manage().Window.Size = new System.Drawing.Size(resolution.width, resolution.height);
                        driver?.Navigate().GoToUrl(url);
                        try
                        {
                            Image WebSite_Image = GetEntireScreenshot(driver, 1920, 1080);
                            if (WebSite_Image != null)
                            {
                                string screenshotFileName = $"{url.Replace("https://", "").Replace("/", "_")}_{resolution.width}x{resolution.height}_{browser}.JPEG";
                                WebSite_Image.Save(screenshotDirectory + screenshotFileName);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }

                driver?.Quit(); // Закрывает браузер после завершения работы с ним
            }
        }
        private Image GetEntireScreenshot(IWebDriver my_Browser, int width, int height)
        {
            try
            {
                // Закрытие уведомления о куках, если оно отображается
                if (driver.FindElement(By.CssSelector("#cookiesAlert > div > div > div > div.cookies-alert-controll > div > button")).Displayed)
                    driver.FindElement(By.CssSelector("#cookiesAlert > div > div > div > div.cookies-alert-controll > div > button")).Click();
                // Прокрутка к началу страницы
                ((IJavaScriptExecutor)my_Browser).ExecuteScript("window.scrollTo(0,0)");
                Thread.Sleep(1000);

                // Получение размеров страницы
                var totalWidth = width;
                var totalHeight = (int)(long)((IJavaScriptExecutor)my_Browser).ExecuteScript("return document.body.parentNode.scrollHeight");

                var viewportHeight = (int)(long)((IJavaScriptExecutor)my_Browser).ExecuteScript("return window.innerHeight");

                // Если размеры страницы меньше или равны размерам окна просмотра
                if (totalWidth <= width && totalHeight <= viewportHeight)
                {
                    var screenshot = my_Browser.TakeScreenshot();
                    return ScreenshotToImage(screenshot);
                }

                // Список для хранения прямоугольников для скриншотов
                var rectangles = new List<Rectangle>();

                for (int y = 0; y < totalHeight; y += viewportHeight)
                {

                    var newHeight = Math.Min(viewportHeight, totalHeight - y);
                    var currRect = new Rectangle(0, y, width, newHeight);
                    rectangles.Add(currRect);
                }

                // Создание итогового изображения
                var stitchedImage = new Bitmap(totalWidth, totalHeight);

                foreach (var rectangle in rectangles)
                {
                    // Прокрутка страницы на высоту текущего прямоугольника
                    ((IJavaScriptExecutor)my_Browser).ExecuteScript($"window.scrollTo(0, {rectangle.Y})");
                    Thread.Sleep(1000); // Подождите немного для загрузки контента

                    // Получение скриншота текущей видимой области
                    var screenshot = my_Browser.TakeScreenshot();
                    var screenshotImage = ScreenshotToImage(screenshot);

                    // Рисование текущего скриншота на итоговом изображении
                    using (var graphics = Graphics.FromImage(stitchedImage))
                    {
                        graphics.DrawImage(screenshotImage, rectangle);
                    }
                }

                return stitchedImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                var screenshot = my_Browser.TakeScreenshot();
                return ScreenshotToImage(screenshot);
            }
        }
        private Image ScreenshotToImage(Screenshot screenshot1)
        {
            Image screenshotImage;
            using(var memStream = new MemoryStream(screenshot1.AsByteArray)) 
            {
                screenshotImage = Image.FromStream(memStream);
            }
            return screenshotImage;
        }

        [TearDown]
        public void Teardown()
        {
            driver?.Dispose(); // Освобождает ресурсы
            try
            {
                Process[] processlist = Process.GetProcesses();
                foreach (Process process in processlist)
                {
                    if (process.ProcessName.Contains("chrome"))
                    {
                        process.Kill();
                    }
                    if (process.ProcessName.Contains("rundll32.exe"))
                    {
                        process.Kill();
                    }
                    if (process.ProcessName.Contains("msedgewebview2.exe"))
                    {
                        process.Kill();
                    }
                }
            }
            catch (Exception)
            {

            }
        }
        //закрыть процесс chrome
        public void KillChromedriverProcess()
        {
            ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/F /IM chromedriver.exe")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(psi)?.WaitForExit();
        }
        //закрыть процесс firefox
        public void KillFirefoxProcess()
        {
            ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/F /IM geckodriver.exe")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(psi)?.WaitForExit();
        }
        //закрыть процесс edge
        public void KillEdgeProcess()
        {
            ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/F /IM msedgewebview2.exe")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(psi)?.WaitForExit();
        }
    }
}