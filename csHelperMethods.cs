using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.IO; //StreamWriter
using System.Diagnostics;
using HtmlAgilityPack;
using System.Security.Policy;
using System.Security.Cryptography;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.Reflection;
using System.Windows.Documents;
using static WebCrawler.csHelperMethods;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
using Newtonsoft.Json.Linq;



//&& Namespace usage example in the project. (2022110804)
namespace WebCrawler
{
    public static class csHelperMethods
    {
        public static int irCrawledUrlCount = 0;
        public static int irDiscoveredUrlCount = 0;



        //database'i temizleme
        //**Bu fonksiyon, veritabanındaki tblMainUrls tablosunun içeriğini boşaltmak için kullanılır. İçerisinde, "DBCrawling" isimli veritabanı bağlantısı açılır ve onun içindeki ObjectContext nesnesine ulaşılır. Daha sonra "ExecuteStoreCommand" metodu ile "truncate table tblMainUrls" SQL komutu çalıştırılır ve tüm veriler silinir.**//

        //&& Using Statement (2022110830)
        public static void clearDatebase()
        {
            using (var context = new DBCrawling())
            {
                var ctx = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)context).ObjectContext;
                ctx.ExecuteStoreCommand("truncate table tblMainUrls");
            }
        }



        /**
        Bu kod, crawlingResult adında bir sınıf tanımlar. Bu sınıf, tblMainUrl sınıfını miras alır, yani tblMainUrl sınıfındaki tüm özelliklere ve davranışlara sahiptir. 
        tblMainUrl sınıfı, web sitelerinin URL'leri gibi temel özelliklerin depolanması ve işlenmesi için kullanılır.
        crawlingResult sınıfı, tblMainUrl sınıfının özelliklerine ek olarak, birkaç özel özellik içerir. Bunlar:
        blcrawlSuccess : Bu özellik, sayfanın başarıyla indirilip indirilemediğini belirtir. true ise sayfa başarıyla indirilmiş, false ise başarısız olmuştur.
        lstDiscoveredLinks: Bu özellik, sayfada keşfedilen bağlantıların listesi olarak tutulur. Bu, keşfedilen URL'leri daha sonra işlemek için kullanılabilir.
        crawlingResult sınıfının yapıcısı, sınıfın özelliklerini başlangıç ​​değerleriyle ayarlar. LastCrawlingDate özelliği için varsayılan değer olarak 1900 yılı belirlenmiştir.
        IsCrawled,CompressionPercent, FetchTimeMS, LinkDepthLevel, PageTitle, SourceCode, Url, UrlHash, DiscoverDate, ParentUrlHash ve CrawlTryCounter özellikleri için varsayılan değerler
        belirtilmemiştir, bu nedenle bunlar sıfır veya null değerleriyle başlatılacaktır.*/


        //&& Class inheritance usage (2022110810) 
        //&& This keyword usage example(2022110806)
        //&& Default constructor and overloaded constructor usage in classes (Polymorphism) (2022110802)
        public class crawlingResult : tblMainUrl      //crawlingResult, tblMainUrl sınıfını miras alacak
        {
            //Varsayılan değerleri yalnızca crawlingResult sınıfının yapıcısında değiştirebiliyoruz.
            public crawlingResult()
            {

                this.LastCrawlingDate = new DateTime(1900, 01, 01); //varsayılan değer, olmasını istediğimiz değer
                this.IsCrawled = false;
                this.CompressionPercent = 0;
                this.FetchTimeMS = 0;
                this.LinkDepthLevel = 0;
                this.PageTitle = null;
                this.SourceCode = null;
                this.Url = "";
                this.UrlHash = "";
                this.DiscoverDate = DateTime.Now;
                this.ParentUrlHash = "";
                this.CrawlTryCounter = 0;
            }

            public bool blcrawlSuccess = true;
            public List<string> lstDiscoveredLinks = new List<string>(); //Keşfedilen bağlantıları liste olarak tutabiliriz;
        }

        //Bu bizim kullanacağımız sınıfımız olacak ve ihtiyacımız olan şey, crawling sonucunu (crawlingResult)
        //döndürecek bir crawling yöntemi oluşturacağız.


        //&& Timers (2022110841)
        //&& TimeSpan, StopWatch (2022110840)
        //&& IsNullOrEmpty (2022110839)
        //&& Ref keyword in methods (2022110834)


        public static void crawlPage(string srUrlToCrawl, int irUrlDepthLevel, string _srParentUrl, DateTime _dtDiscoverDate)
        {
            var vrLocalUrl = srUrlToCrawl;
            crawlingResult crawResult = new crawlingResult();
            crawResult.Url = vrLocalUrl;
            if (!string.IsNullOrEmpty(_srParentUrl))
                crawResult.ParentUrlHash = _srParentUrl;
            if (_dtDiscoverDate != DateTime.MinValue)
                crawResult.DiscoverDate = _dtDiscoverDate;

            Stopwatch swTimerCrawling = new Stopwatch();
            swTimerCrawling.Start();


            HtmlWeb wbClient = new HtmlWeb();             //you should use httpwebrequest for more control and better performance

            wbClient.AutoDetectEncoding = true;
            wbClient.BrowserTimeout = new TimeSpan(0, 2, 0); //2 dakika
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            try
            {
                doc = wbClient.Load(crawResult.Url);
                crawResult.SourceCode = doc.Text; //kaynak kodu ancak ham kod değil inşa edilmiş kaynak kodudur.

                //+++
                // Sayfa başlığı için XPath ifadesi kullanarak etiketi seçiyoruz.
                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                if (titleNode != null)
                {
                    crawResult.PageTitle = titleNode.InnerHtml;
                }
            }
            catch (Exception E)
            {
                crawResult.blcrawlSuccess = false;
                //Url taranırken bir hata olursa, yapacağımız şey log hatasını (logeError) çağırmak
                logError(E, "crawlPage");
            }
            Interlocked.Increment(ref irCrawledUrlCount);

            swTimerCrawling.Stop();
            crawResult.FetchTimeMS = Convert.ToInt32(swTimerCrawling.ElapsedMilliseconds);
            crawResult.LastCrawlingDate = DateTime.Now;
            saveCrawInDatabase(crawResult);

            if (crawResult.blcrawlSuccess)
            {
                extractLinks(crawResult, doc);
                saveDiscoveredLinksInDatabaseForFutureCrawling(crawResult);
            }
            doc = null;
        }



        private static object _lockDatabaseAdd = new object();


        //&& Lock usage (2022110827)
        //&& Dictionary and HashSet (2022110820)
        //&& Ref keyword in methods (2022110834)
        private static void saveDiscoveredLinksInDatabaseForFutureCrawling(crawlingResult crawResult)
        {
            lock (_lockDatabaseAdd)
            {


                using (var context = new DBCrawling())
                {
                    HashSet<string> hsProcessedUrls = new HashSet<string>();

                    foreach (var vrPerLink in crawResult.lstDiscoveredLinks)
                    {
                        var vrHashedLink = vrPerLink.ComputeHashOfOurSystem();
                        if (hsProcessedUrls.Contains(vrHashedLink))
                            continue;
                        var vrResult = context.tblMainUrls.Any(databaseRecords => databaseRecords.UrlHash == vrHashedLink); /////1.20.11 

                        if (vrResult == false)
                        {
                            crawlingResult newLinkCrawlingResult = new crawlingResult();
                            newLinkCrawlingResult.Url = vrPerLink.normalizeUrl();
                            newLinkCrawlingResult.HostUrl = newLinkCrawlingResult.Url.returnRootUrl();

                            newLinkCrawlingResult.UrlHash = vrPerLink.ComputeHashOfOurSystem(); //**01.45.00
                            newLinkCrawlingResult.ParentUrlHash = crawResult.UrlHash;
                            newLinkCrawlingResult.LinkDepthLevel = (short)(crawResult.LinkDepthLevel + 1);
                            context.tblMainUrls.Add(newLinkCrawlingResult.converToBaseMainUrlClass());
                            hsProcessedUrls.Add(vrHashedLink);
                            Interlocked.Increment(ref irDiscoveredUrlCount);
                        }
                    }
                    context.SaveChanges();
                }
            }
        }





        //Sonuçları işleyip veritabanına kaydetme
        ///sonuçları veritabanına kaydedebilmek için veritabanı sınıfımızın bir örneğini oluşturmamız gerekiyor.
        //&& Lock usage (2022110827)
        //&& IsNullOrEmpty (2022110839)
        private static void saveCrawInDatabase(crawlingResult crawledResult)
        {
            lock (_lockDatabaseAdd)
            {
                using (var context = new DBCrawling())
                {
                    crawledResult.UrlHash = crawledResult.Url.ComputeHashOfOurSystem();
                    crawledResult.HostUrl = crawledResult.Url.returnRootUrl();
                    var vrResult = context.tblMainUrls.SingleOrDefault(b => b.UrlHash == crawledResult.UrlHash);
                    crawledResult.ParentUrlHash = crawledResult.ParentUrlHash.ComputeHashOfOurSystem();

                    if (crawledResult.blcrawlSuccess == true)
                    {
                        crawledResult.IsCrawled = true;
                        if (!string.IsNullOrEmpty(crawledResult.SourceCode))
                        {
                            double dblOriginalSourceCodeLenght = crawledResult.SourceCode.Length;
                            crawledResult.SourceCode = crawledResult.SourceCode.CompressString();
                            crawledResult.CompressionPercent = Convert.ToByte(
                                Math.Floor(
                                    (crawledResult.SourceCode.Length.ToDouble() / dblOriginalSourceCodeLenght) * 100)
                                );
                        }
                        crawledResult.CrawlTryCounter = 0;


                    }

                    tblMainUrl finalObject = crawledResult.converToBaseMainUrlClass();


                    //this approach brings extra overload to the server with deleting from server first
                    //therefore will use copy properties of object to another object without changing reference
                    //   if(vrResult != null)
                    //    {
                    //        context.tblMainUrls.Remove(vrResult);
                    //        context.SaveChanges();
                    //    }
                    //    context.tblMainUrls.Add(finalObject);
                    //    var gg = context.SaveChanges();
                    //}


                    if (vrResult != null)
                    {
                        finalObject.DiscoverDate = vrResult.DiscoverDate;
                        finalObject.LinkDepthLevel = vrResult.LinkDepthLevel;
                        finalObject.CrawlTryCounter = vrResult.CrawlTryCounter;


                        if (crawledResult.blcrawlSuccess == false)
                            finalObject.CrawlTryCounter++;

                        finalObject.CopyProperties(vrResult);

                    }
                    else
                        context.tblMainUrls.Add(finalObject);

                    var gg = context.SaveChanges();
                }
            }
        }


        //&& This keyword usage example (2022110806)
        private static tblMainUrl converToBaseMainUrlClass(this tblMainUrl finalObject)
        {
            return JsonConvert.DeserializeObject<tblMainUrl>(JsonConvert.SerializeObject(finalObject));
        }



        /**Bu method iki nesnenin özelliklerini birbirine kopyalar. İlk olarak kaynak ve hedef nesnenin null olup olmadığı kontrol edilir. Eğer herhangi bir nesne null ise bir hata fırlatılır. Daha sonra kaynak ve hedef nesnenin tipi alınır ve hedef nesne içinde bulunan her bir özelliğin kaynak nesnede bulunup bulunmaması kontrol edilir. Eğer kaynak nesnede bulunuyorsa, kaynak nesnenin bu özelliği hedef nesnenin aynı isimli özelliğine atanır. Böylece iki nesnenin özellikleri birbirine kopyalanır.**/
        //Source code: https://stackoverflow.com/questions/930433/apply-properties-values-from-one-object-to-another-of-the-same-type-automaticall

        //&&This keyword usage example (2022110806)
        public static void CopyProperties(this object source, object destination)
        {
            // If any this null throw an exception / (If any this null throw an exception)
            if (source == null || destination == null)
                throw new Exception("Source or/and Destination Objects are null");
            // Getting the Types of the objects /(Nesne türlerini al)
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();
            // Collect all the valid properties to map / (Eşlenecek tüm geçerli özellikleri topla)
            var results = from srcProp in typeSrc.GetProperties()
                          let targetProperty = typeDest.GetProperty(srcProp.Name)
                          where srcProp.CanRead
                          && targetProperty != null
                          && (targetProperty.GetSetMethod(true) != null && !targetProperty.GetSetMethod(true).IsPrivate)
                          && (targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) == 0
                          && targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType)
                          select new { sourceProperty = srcProp, targetProperty = targetProperty };
            //map the properties / (özellikleri eşle)
            foreach (var props in results)
            {
                props.targetProperty.SetValue(destination, props.sourceProperty.GetValue(source, null), null);
            }
        }

        //html kod çözme metodu


        /**Bu C# kodu, verilen URL metninin HTML entity kodlarını çözümlemek için bir özel metod tanımlar. DeEntitize metodu kullanılarak HTML entity kodları çözümlenir ve çözümlenen metin geri döndürülür.
         * 
         * 
         * string url = "&lt;p&gt;Merhaba Dünya!&lt;/p&gt;";
           string decodedUrl = url.decodeUrl();
           Bu kod çalıştırıldıktan sonra, decodedUrl değişkeni şu değeri alacaktır: "<p>Merhaba Dünya!</p>". Yani, &lt;p&gt; ve &lt;/p&gt; gibi HTML entity kodları çözümlenerek, <p> ve </p> şekline dönüştürülür.**/
        //&& This keyword usage example (2022110806)
        private static string decodeUrl(this string srUrl)
        {
            return HtmlEntity.DeEntitize(srUrl);
        }

        //&& IsNullOrEmpty (2022110839)

        private static void extractLinks(crawlingResult myCrawlingResult, HtmlDocument doc)
        {

            //burada yaptığımız şey şu: önce URL dizisinden bir Url oluşturuyoruz, sonra HTML web oluşturuyoruz
            //ve bu Web'den otomotik algılama kodlamasını kullanıyoruz ve HTML belgesi oluşturuyoruz.
            var baseUri = new Uri(myCrawlingResult.Url);

            //(1.24.30)
            // extracting all links  
            var vrNodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (vrNodes != null)
                foreach (HtmlNode link in vrNodes) //xpath notation
                {
                    HtmlAttribute att = link.Attributes["href"];
                    //this is used to convert from relative path to absolute path
                    var absoluteUri = new Uri(baseUri, att.Value.ToString().decodeUrl());

                    if (!absoluteUri.ToString().StartsWith("http://") && !absoluteUri.ToString().StartsWith("https://"))
                        continue;

                    myCrawlingResult.lstDiscoveredLinks.Add(absoluteUri.ToString().Split('#').FirstOrDefault());
                }

            myCrawlingResult.lstDiscoveredLinks = myCrawlingResult.lstDiscoveredLinks.Distinct().Where(pr => pr.Length < 201).ToList(); //link uzunluğunu kontrol etme

            //var vrDocTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.ToString().Trim();
            //vrDocTitle = System.Net.WebUtility.HtmlDecode(vrDocTitle);


            var vrDocTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.ToString().Trim();
            if (!string.IsNullOrEmpty(vrDocTitle))
            {
                vrDocTitle = System.Net.WebUtility.HtmlDecode(vrDocTitle);
                myCrawlingResult.PageTitle = vrDocTitle;
            }
            else
            {
                myCrawlingResult.PageTitle = "Title not found";
            }


            //her zaman null kontrolüne sahip olmamaız gerekir ve bunu HTML kod çözme ile çözeriz çünkü başlık kodlanmış olabilir ve başlığı atar, belgeyi döndürürüz. 

            //myCrawlingResult.PageTitle = vrDocTitle;


        }




        /**Bu kod bloğu, "swErrorLogs" ve "swLog" isimli iki dosya yazma nesnesinin oluşturulduğunu ve bu dosyalardaki verinin hemen yazılması gerektiğini belirtir. "AutoFlush" özelliği "true" olarak ayarlanmıştır, bu da dosyaya yapılan her değişikliğin hemen diske yazılmasını gerektiğini belirtir.**/
        static csHelperMethods()
        {
            swErrorLogs.AutoFlush = true;
            swLog.AutoFlush = true;
        }



        //global bir günlük kaydı (hatalar için) 
        private static StreamWriter swErrorLogs = new StreamWriter("error_logs.txt", append: true, encoding: Encoding.UTF8);
        private static object _lock_swErrorLogs = new object();
        /**
           *logError metodu: Bu metod, bir Exception nesnesi ve hata oluştuğu yöntemin adını alır.
           *lock bloğu: Metod içinde, swErrorLogs nesnesine erişimin senkronize edilmesi için lock bloğu kullanılır. 
           *StreamWriter nesneleri iş parçacıklı değil olduğundan, birden fazla iş parçacığı tarafından aynı anda kullanılırsa hatalar oluşabilir. lock anahtar kelimesi,nesnenin birden fazla iş parçacığı tarafından aynı anda kullanılmasını engeller.
           *swErrorLogs.WriteLine komutları: Hata mesajı, iç hata mesajı (eğer varsa), hata yığını ve iç hata yığını (eğer varsa) gibi bilgiler swErrorLogs nesnesine yazdırılır. Böylece oluşan hataların detaylı bir kaydı tutulabilir.
           *AutoFlush özelliği: swErrorLogs ve swLog nesnelerinin AutoFlush özelliği true olarak ayarlanır. Bu, yazılan verilerin anında dosyaya yazılmasını sağlar ve verilerin bellekte tutulmasını engeller.
           *Bu kod, hata oluştuğunda hata detaylarını tutmak için bir hata günlüğü oluşturmak için kullanılabilir.**/
        //&& Lock usage (2022110827)

        public static void logError(Exception E, string callingMethodName) //Exception E hatanın kendisini gönderiyoruz
        {
            lock (_lock_swErrorLogs)
            // ı am using lock methodology to synchronize access to a non-thread safe  object streamwriter.
            {
                swErrorLogs.WriteLine(callingMethodName + "\t" + DateTime.Now);
                swErrorLogs.WriteLine();
                swErrorLogs.WriteLine(E.Message); //**hata mesajı
                swErrorLogs.WriteLine();
                swErrorLogs.WriteLine(E.InnerException?.Message); //**iç hata mesajı (eğer varsa)
                swErrorLogs.WriteLine();
                swErrorLogs.WriteLine(E?.StackTrace); //**hata yığını /**Hata yığını, hata oluştuğu anda nerede ve hangi fonksiyonlar tarafından çağrıldığı hakkında bilgi verir ve bu bilgi, hata nedenini belirlemede yardımcı olabilir.
                swErrorLogs.WriteLine();
                swErrorLogs.WriteLine(E.InnerException?.StackTrace); //**iç hata yığını (eğer varsa)
                swErrorLogs.WriteLine();
                swErrorLogs.WriteLine("***********************");
                swErrorLogs.WriteLine();
            }
        }


        /**Bu method, verilen URL adresini aşağıdaki işlemleri yaparak normalize eder:
        URL adresindeki boşlukları Trim() metoduyla siler.
        URL adresini CultureInfo("en-US") kullanarak ingilizce küçük harf karakterlerine çevirir.
        Bu işlemler, verilen URL adresini standart hale getirir ve aynı adresin farklı şekillerde yazılmasından kaynaklanan hataları önler.**/

        //&& This keyword usage example (2022110806)
        //&& CultureInfo (2022110837)
        public static string normalizeUrl(this string srUrl)
        {
            return srUrl.ToLower(new System.Globalization.CultureInfo("en-US")).Trim();
        }



        /**Bu kod, verilen string veriyi SHA256 şifreleme algoritması ile şifreler ve sonuç olarak 64 karakterli bir hexadecimal string döndürür. İlk olarak, SHA256 şifreleme nesnesi oluşturulur. Daha sonra, verilen string veri UTF8 kodlaması ile byte dizisine dönüştürülür ve bu byte dizisi SHA256 şifreleme algoritması ile şifrelenir. Son olarak, şifrelenmiş byte dizisi hexadecimal string'e dönüştürülür ve döndürülür.**/
        //Source:https://www.c-sharpcorner.com/article/compute-sha256-hash-in-c-sharp/
        //&& This keyword usage example (2022110806)
        //&& StringBuilder(2022110833)

        private static string ComputeSha256Hash(this string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }



        /**Bu metod, verilen URL'nin kök URL'sini döndürür. Örneğin, verilen URL "https://www.example.com/page1" olsun, metod "www.example.com" döndürür. Bunu yapmak için, bir Uri nesnesi oluşturulur ve uri.Host özelliği kullanılır.**/
        //&& This keyword usage example (2022110806)
        public static string returnRootUrl(this string srUrl)
        {
            var uri = new Uri(srUrl); //**Uri (Uniform Resource Identifier), bir URL'nin veya URN'nin (Uniform Resource Name) tanımlanmasına yarayan bir veri tipidir. Uri veri tipi, internet üzerindeki bir dosya, kaynak veya verinin adresini tanımlamaya yarar.*/
            return uri.Host; //**Host, bir URL'de verilen bir internet kaynağının barındığı sunucunun adıdır. Örneğin, bir URL http://www.google.com olsun, host "www.google.com" olacaktır.**/
        }



        /**Bu metod srUrl adlı bir string'in normalize edilmiş hali üzerinden bir SHA256 hash hesaplaması yapar. Öncelikle srUrl değişkeninin normalizeUrl metodu ile lowercase ve trim işlemi yapılır. Daha sonra, hesaplanacak olan hash değeri için ComputeSha256Hash metodu kullanılır ve hesaplanan hash değeri döndürülür.**/
        //&& This keyword usage example (2022110806)
        static string ComputeHashOfOurSystem(this string srUrl)
        {
            return srUrl.normalizeUrl().ComputeSha256Hash();
        }




        /**
            Bu kod, bir log dosyası oluşturmak ve bu dosyaya mesajlar yazmak için kullanılan bir metodu tanımlar.

           *StreamWriter swLog = new StreamWriter("logs.txt", true, Encoding.UTF8): 
            Bu satır, UTF8 kodlamasıyla "logs.txt" isimli bir log dosyası oluşturan bir StreamWriter nesnesi tanımlar. true değeri, dosyanın sonuna yazılmasını sağlar ve eski veriler silinmez.

           *private static object _lock_swLogs = new object(): 
            Bu satır, birden fazla iş parçacığının log dosyasına eş zamanlı olarak yazmasını önlemek için kullanılan bir kilitleme nesnesi tanımlar.

           *public static void logMessage(string srMsg): 
            Bu metod, log dosyasına mesaj yazmak için kullanılır. Metod içinde, lock yapısı ile kilitleme nesnesi kullanılır ve log dosyasına zaman ve mesaj bilgisi yazılır.
        **/

        private static StreamWriter swLog = new StreamWriter("logs.txt", true, Encoding.UTF8);
        private static object _lock_swLogs = new object();

        //&& Lock usage (2022110827)

        public static void logMessage(string srMsg)
        {
            lock (_lock_swLogs)
            {
                swLog.WriteLine($"{DateTime.Now}\t\t{srMsg}");
            }
        }




        ///////////////**********************************************************////////////////////////////////





    }
}
