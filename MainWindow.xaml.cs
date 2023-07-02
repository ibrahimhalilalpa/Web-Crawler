using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static WebCrawler.csHelperMethods;


//&& Namespace usage example in the project. (2022110804)
namespace WebCrawler
{
    /// <summary>
    /// MainWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class MainWindow : Window
    {

        /**Bu kod satırları, yapılan web tarama işlemlerinin aynı anda 20 adet yapılabileceğini ve en fazla 3 deneme yapılması gerektiğini belirtir. "_irnumberOfTotalConcurrentCrawling" değişkeni, aynı anda yapılabilecek tarama sayısını belirtirken, "_irMaximumTryCount" değişkeni ise tarama işleminin başarısız olması durumunda yapılacak maksimum deneme sayısını belirtir.**/
        private static int _irnumberOfTotalConcurrentCrawling = 20;
        private static int _irMaximumTryCount = 3;


        /**Bu kod, bir veri yapısı olan ObservableCollection türünde bir özelliğin tanımlanmasını içerir. Özellik adı "UserLogs" ve türü "ObservableCollection<string>" dir. Bu veri yapısı, dinamik olarak değişen bir veri koleksiyonunu tutar ve herhangi bir değişikliği otomatik olarak ekrandaki görüntüleme nesnesine yansıtır.Bu özellik, kodun diğer bölümlerinde kullanıcı tarafından giriş yapılan verilerin tutulması ve görüntülenmesi için kullanılabilir.**/
        
        
        private ObservableCollection<string> _Results = new ObservableCollection<string>();
        public ObservableCollection<string> UserLogs
        {
            get { return _Results; }
            set
            {
                _Results = value;
            }
        }

        /**
         Bu kodlar bir WPF uygulamasının MainWindow sınıfının constructor methodu içerisinde yer alır.

         InitializeComponent() methodu, WPF uygulamalarında görsel elemanların oluşturulması için kullanılan bir methoddur.
         ThreadPool.SetMaxThreads(100000, 100000) ve ThreadPool.SetMinThreads(100000, 100000) methodları, 
         ThreadPool'un maksimum ve minimum çalıştırılabilecek thread sayısını 100000 olarak ayarlar.
         ServicePointManager.DefaultConnectionLimit = 1000; bu satır, aynı anda bir host'a bağlanabilecek maksimum bağlantı sayısını 1000 olarak ayarlar.
l        istBoxResults.ItemsSource = UserLogs bu satır, WPF uygulamalarında kullanılan bir liste nesnesinin (listBoxResults) veri kaynağını (UserLogs) ayarlar.
        **/
        public MainWindow()
        {
            InitializeComponent();
            //threadpools allows you to start as many as threads you want immediately (iş parçacığı havuzları, hemen istediğiniz kadar iş parçacığı başlatmanıza izin verir)
            ThreadPool.SetMaxThreads(100000, 100000);
            ThreadPool.SetMinThreads(100000, 100000);
            ServicePointManager.DefaultConnectionLimit = 1000; //this increases your number of connections to per host at the same time (bu, aynı anda ana bilgisayar başına bağlantı sayınızı artırır)
            listBoxResults.ItemsSource = UserLogs;
        }
        DateTime dtStartDate;


        //Database'in çalışıp çalışmadığını kontrol etme ve manuel veri girişi.
        /**Bu kod parçası, "btnTest_Click" isimli bir düğme tıklama olayındaki işlemleri tanımlar. İçindeki kod, "DBCrawling" isimli veritabanını kullanarak tüm kayıtların silinmesini ve yeni bir kayıt eklenmesini içerir. 
        İlk olarak, veritabanındaki tüm kayıtlar "RemoveRange" metodu ile silinir. 
        Daha sonra, "Add" metodu ile "tblMainUrls" tablosuna yeni bir kayıt eklenir. 
        Son olarak, yapılan değişiklikler "SaveChanges" metodu ile veritabanına kaydedilir.*/
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {

            using (DBCrawling db = new DBCrawling())

            {
                //**remove data from database / (veritabanındaki verileri kaldırma)
                /*
                 * var all = db.tblMainUrls.Select(pr => pr);
                db.tblMainUrls.RemoveRange(all);
                db.SaveChanges();
                */ //(working)

                //**remove data from database / (veritabanındaki verileri kaldırma)

                //(1)
                db.tblMainUrls.RemoveRange(db.tblMainUrls);
               //(2)
                //foreach (var vrDBData in db.tblMainUrls)
                //{
                //    db.tblMainUrls.Remove(vrDBData);
                //}

                //**Saving the changes / (Değişiklikleri kaydetme)
                db.SaveChanges();

                //**Manual data entry into database (trial) / (Veritabanına manuel veri girişi (deneme))

                db.tblMainUrls.Add(new tblMainUrl { 
                    UrlHash= "xx",
                    Url = "www.toros.edu.tr",
                    DiscoverDate = new DateTime(1881, 01, 01),
                    LinkDepthLevel = 1,
                    ParentUrlHash = "hash",
                    LastCrawlingDate = new DateTime(1938, 11, 10),
                    SourceCode = "hh",
                    FetchTimeMS = 300,
                    PageTitle = "Page title",
                    CompressionPercent = 20,
                    IsCrawled = true,
                    HostUrl = "kk",
                    CrawlTryCounter = 1
                });

                //**Saving the changes / (Değişiklikleri kaydetme)
                db.SaveChanges();

                MessageBox.Show("Database operations completed.");
            }
        }


        //+// Database'i temizleme işlemi
        private void btnClearDB_Click(object sender, RoutedEventArgs e)
        {
            //(1)
            //csHelperMethods.clearDatebase();

            //(2)

            clearDatebase(); //( //using static WebCrawler.csHelperMethods;)
 

            MessageBox.Show("All data in database deleted!");    

        }






        /*
        //**********************************************************************************************************************************************
        //+// Taramayı durdurma

        private bool _stopCrawling = false;

        private void StartCrawling()
        {
            while (!_stopCrawling)
            {
                // Perform web crawling logic here
            }
        }


        private int _crawlingStatus = 0;
        DispatcherTimer xx = null;


        //&& Timers (2022110841)
        private void btnStopCrawling_Click(object sender, RoutedEventArgs e)
        {
            if (_crawlingStatus == 1)
            {
                _crawlingStatus = 0;
                btnStopCrawling.Content = "Start";

                if (_timer != null)
                {
                    _timer.Stop();
                }


            }
            else
            {
                MessageBox.Show("başladı");

                _crawlingStatus = 1;
                btnStopCrawling.Content = "Stop";
                crawlPage(txtInputUrl.Text.normalizeUrl(), 0, txtInputUrl.Text.normalizeUrl(), DateTime.Now); ///HATAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
                checkingTimer();

            }

        }

        private DispatcherTimer _timer;*/

        //**********************************************************************************************************************************************



        //**Bu kod bir WPF (Windows Presentation Foundation) formundaki bir butona tıklama olayı için tanımlanmış bir olay işleyicisidir. Tıklama olayı gerçekleştiğinde, aşağıdaki işlemler gerçekleşir:
        //dtStartDate değişkeni, o anki tarih ve saat değerini alır.
        //crawlPage metodu çağrılır ve txtInputUrl adlı metin kutusundaki URL değeri normalizeUrl metodu ile normalize edilerek metodun ilk parametresi olarak verilir.
        //Diğer parametreler ise sırasıyla 0, txtInputUrl metin kutusundaki URL değeri, o anki tarih ve saat değeridir.
        //checkingTimer metodu çağrılır.


        //&& Timers (2022110841)
        private void btnCrawl_Click(object sender, RoutedEventArgs e)
        {
            dtStartDate = DateTime.Now;
            crawlPage(txtInputUrl.Text.normalizeUrl(), 0, txtInputUrl.Text.normalizeUrl(), DateTime.Now); ///HATAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            checkingTimer();
        }



        //&& Timers (2022110841)
        //&& TimeSpan, StopWatch (2022110840)
        private void checkingTimer()
        {
            {
                //source:https://stackoverflow.com/questions/11559999/how-do-i-create-a-timer-in-wpf
                System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(startPollingAwaitingURLs);
                dispatcherTimer.Interval = new TimeSpan(0,0,0,0,1000);
                dispatcherTimer.Start();
            }
        }

        private static object _lock_CrawlingSync = new object();
        private static bool blBeingProcessed = false;
        private static List<Task> lstCrawlingTasks = new List<Task>();
        private static List<string> lstCurrentlyCrawlingUrls = new List<string>();




        //&& Lock usage (2022110827)

        private void startPollingAwaitingURLs(object sender, EventArgs e)
        {

            lock(UserLogs)
            {
                string srPerMinCrawlingSpeed = (irCrawledUrlCount.ToDouble() /  (DateTime.Now-dtStartDate).TotalMinutes).ToString("N2");
                string srPerMinDiscoveredLinkSpeed = (irDiscoveredUrlCount.ToDouble() / (DateTime.Now - dtStartDate).TotalMinutes).ToString("N2");
                string srPassedTime = (DateTime.Now - dtStartDate).TotalMinutes.ToString("N2");

                UserLogs.Insert(0, $"{DateTime.Now}polling awaiting urls \t processing: {blBeingProcessed} \t number of crawling tasks: {lstCrawlingTasks.Count}");

                UserLogs.Insert(0, $"Total Time: {srPassedTime} Minutes \t Total Crawled Links Count: {irCrawledUrlCount.ToString("N0")} \t Crawling Speed Per Minute: {srPerMinCrawlingSpeed} \t Total Discovered Links: {irDiscoveredUrlCount.ToString("N0")} \t Discovered Url Speed: {srPerMinDiscoveredLinkSpeed}");

            }


            logMessage($"polling awaiting urls \t processing: {blBeingProcessed} \t number of crawling tasks: {lstCrawlingTasks.Count}");

            if (blBeingProcessed)
                return;
            lock(_lock_CrawlingSync)
            {
                blBeingProcessed = true;

                lstCrawlingTasks = lstCrawlingTasks.Where(pr => pr.Status != TaskStatus.RanToCompletion && pr.Status != TaskStatus.Faulted).ToList();

                int irTasksCountToStart = _irnumberOfTotalConcurrentCrawling - lstCrawlingTasks.Count;
                if(irTasksCountToStart>0)

                using (DBCrawling db = new DBCrawling())

                {
                  var vrReturnedList =  db.tblMainUrls.Where(x => x.IsCrawled == false && x.CrawlTryCounter<_irMaximumTryCount)
                        .OrderBy(pr=>pr.DiscoverDate)
                        .Select(x => new
                        {
                         x.Url,
                         x.LinkDepthLevel
                        }).Take(irTasksCountToStart*2).ToList();

                        logMessage(string.Join(" , ", vrReturnedList.Select(pr => pr.Url)));

                    foreach (var vrPerReturned in vrReturnedList)
                    {
                            var vrUrlToCrawl = vrPerReturned.Url;
                            int irDepth = vrPerReturned.LinkDepthLevel;

                            lock (lstCurrentlyCrawlingUrls)
                            {
                                if (lstCurrentlyCrawlingUrls.Contains(vrUrlToCrawl))
                                {
                                    logMessage($"bypass url since already crawling: \t {vrUrlToCrawl}");
                                    continue;
                                }
                                lstCurrentlyCrawlingUrls.Add(vrUrlToCrawl);
                            }

                            logMessage($"starting crawling url : \t {vrUrlToCrawl}");

                            lock (UserLogs)
                            {
                                UserLogs.Insert(0, $"{DateTime.Now} starting crawling url : \t {vrUrlToCrawl}");
                            }



                            var vrStartedTask = Task.Factory.StartNew(() => { crawlPage(vrUrlToCrawl, irDepth, null, DateTime.MinValue); }).ContinueWith((pr) =>
                            {
                                lock(lstCurrentlyCrawlingUrls)
                                {
                                    lstCurrentlyCrawlingUrls.Remove(vrUrlToCrawl);
                                    logMessage($"removing url from list since task completed: \t {vrUrlToCrawl}");
                                }
                            });
                            lstCrawlingTasks.Add(vrStartedTask);

                            if (lstCrawlingTasks.Count > _irnumberOfTotalConcurrentCrawling)
                                break;
                    }
                }
                blBeingProcessed = false;
            }
        }




        /********************************************************************************************************************************/

        //**// ComboBox'daki seçili öğeyi text box'a yazdırma.
        private void ComboBoxUrls_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedItem = ComboBoxUrls.SelectedItem as ComboBoxItem;
            txtInputUrl.Text = selectedItem.Content.ToString();
        }

        //**// TextBox'a çift tıklandığında içindeki yazıyı silme.

        private void txtInputUrl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            txtInputUrl.Text = "";
        }



    }

}
