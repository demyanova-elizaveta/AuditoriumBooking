using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScheduleInfo;
using AuditoriumBooking.SharedResources;
using Microsoft.Win32;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using AuditoriumBooking.Helpers;
using System.Xml;
using Google.Apis.Upload;
using System.Windows.Shapes;

namespace AuditoriumBooking.DayScheduler
{
    public partial class DayScheduler : Window
    {
        private string[] Scopes = { DriveService.Scope.Drive };
        private string ApplicationName = "AuditoriumBooking";
        private string FolderId = "";
        private string contentType = "application/xml";
        private int cellWidth = 110;
        private int cellHeight = 60;

        private Dictionary<string,string> FileIDs;

        private DriveService driveService;

        public DayScheduler()
        {
            InitializeComponent();

            using (var sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "folderID.txt"))
            {
                FolderId = sr.ReadToEnd();
            }
            
            ResourcesProvider.Current.ChosenDate = DateTime.Now;
            ResourcesProvider.Current.FilterWeek = Week.Both;

            //Characheristics for datePicker
            var calendarDateRange = new CalendarDateRange(DateTime.MinValue, DateTime.Today.AddDays(-1));
            datePicker.BlackoutDates.Add(calendarDateRange);
            datePicker.DataContext = ResourcesProvider.Current.ChosenDate;
            
            //Subscription on events
            classesTitles.MouseWheel += new MouseWheelEventHandler(DisableMouseWheel);
            timesTitles.MouseWheel += new MouseWheelEventHandler(DisableMouseWheel);

            UserCredential credential = GetUserCredentials();
            driveService = GetDriveService(credential);

            //Googe Drive API
            FilesResource.ListRequest listRequest = driveService.Files.List();
            listRequest.Q = "'" + FolderId + "' in parents and trashed=false";
            listRequest.Fields = "nextPageToken, files(*)";

            try
            {
                IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;

                FileIDs = new Dictionary<string, string>();

                foreach (var file in files)
                {
                    FileIDs[file.Name] = file.Id;
                }

                if (files.Count == 0)
                {
                    ShowInitialCard();
                }
                else
                {
                    DownloadFileFromDrive(FileIDs);
                }
            }
            catch(Exception)
            {
                //System.Windows.Forms.MessageBox.Show("Перезапуск.");
                System.Windows.Forms.Application.Restart();
                Environment.Exit(0);
            }
        }

        //Drive Service And Credentials
        private DriveService GetDriveService(UserCredential credential)
        {
            // Create Drive API service.
            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        private UserCredential GetUserCredentials()
        {
            UserCredential credential;
            using (var stream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "client_secret.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = AppDomain.CurrentDomain.BaseDirectory + "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            return credential;
        }

        //Serialization/deserialization
        private byte[] SerializeXmlFormat(string fileName)
        {
            using (var stream = new MemoryStream())
            {
                XmlSerializer serializer;
                switch (fileName)
                {
                    case "AuditoriumScheduleData.xml":
                        serializer = new XmlSerializer(typeof(ScheduleDictionaryItem[]));
                        serializer.Serialize(stream, ResourcesProvider.Current.Auditoriums.Select(kv => new ScheduleDictionaryItem() { ID = kv.Key, Value = kv.Value }).ToArray());
                        break;
                    case "AuditoriumApplicationsData.xml":
                        serializer = new XmlSerializer(typeof(ApplicationDictionaryItem[]));
                        serializer.Serialize(stream, ResourcesProvider.Current.Applications.Select(kv => new ApplicationDictionaryItem() { ID = kv.Key, Value = kv.Value }).ToArray());
                        break;
                    case "ClassroomsData.xml":
                        serializer = new XmlSerializer(typeof(List<Classroom>));
                        serializer.Serialize(stream, ResourcesProvider.Current.Classrooms);
                        break;
                    case "PeriodsData.xml":
                        serializer = new XmlSerializer(typeof(List<Period>));
                        serializer.Serialize(stream, ResourcesProvider.Current.Periods);
                        break;
                }
                stream.Position = 0;
                var br = new BinaryReader(stream);
                return br.ReadBytes((int)stream.Length);
            }
        }

        private void DeserializeXMLFormat(XmlDocument xmlDocument, string fileName)
        {
            var xmlString = xmlDocument.OuterXml;

            using (StringReader read = new StringReader(xmlString))
            {
                XmlSerializer serializer;
                using (var reader = new XmlTextReader(read))
                {
                    switch (fileName)
                    {
                        case "AuditoriumScheduleData.xml":
                            serializer = new XmlSerializer(typeof(ScheduleDictionaryItem[]));
                            ResourcesProvider.Current.Auditoriums = ((ScheduleDictionaryItem[])serializer.Deserialize(reader)).ToDictionary(i => i.ID, i => i.Value);
                            ClearOldCancelledDates(ResourcesProvider.Current.Auditoriums.ToDictionary(x => x.Key, x => x.Value as IAuditoriumInfo));
                            break;
                        case "AuditoriumApplicationsData.xml":
                            serializer = new XmlSerializer(typeof(ApplicationDictionaryItem[]));
                            ResourcesProvider.Current.Applications = ((ApplicationDictionaryItem[])serializer.Deserialize(reader)).Where(item => item.Value.ParticularDate >= DateTime.Today).ToDictionary(i => i.ID, i => i.Value);
                            ClearOldCancelledDates(ResourcesProvider.Current.Applications.ToDictionary(x => x.Key, x => x.Value as IAuditoriumInfo));
                            break;
                        case "ClassroomsData.xml":
                            serializer = new XmlSerializer(typeof(List<Classroom>));
                            ResourcesProvider.Current.Classrooms = (List<Classroom>)serializer.Deserialize(reader);
                            break;
                        case "PeriodsData.xml":
                            serializer = new XmlSerializer(typeof(List<Period>));
                            ResourcesProvider.Current.Periods = (List<Period>)serializer.Deserialize(reader);
                            break;
                    }
                }
            }
        }

        //Cleaning of old events
        private void ClearOldCancelledDates(Dictionary<Guid, IAuditoriumInfo> dictionary)
        {
            foreach (var item in dictionary)
            {
                for (int i = 0; i < item.Value.CancelledDates.Count; i++)
                {
                    if (item.Value.CancelledDates[i] < DateTime.Now.Date)
                    {
                        var updatedDaysList = item.Value.CancelledDates;
                        updatedDaysList.RemoveAt(i);
                        item.Value.CancelledDates = updatedDaysList;
                    }
                }
                if (item.Value.SpecifiedColors.Count > 0)
                {
                    for (int j = 0; j < item.Value.SpecifiedColors.Count; j++)
                    {
                        if (item.Value.SpecifiedColors[j].Key < DateTime.Now.Date)
                        {
                            var specificColorsList = item.Value.SpecifiedColors;
                            specificColorsList.RemoveAt(j);
                            item.Value.SpecifiedColors = specificColorsList;
                        }
                    }
                }

                //Cleaning of events that have End date
                var currentEvent = item.Value as ApplicationInfo;

                if (currentEvent != null && !currentEvent.IsWeeklyEvent && currentEvent.ParticularDate < DateTime.Now.Date)
                {
                    ResourcesProvider.Current.Applications.Remove(item.Key);
                }
            }
        }

        //Downloading and uploading drive files
        private void DownloadFileFromDrive(Dictionary<string, string> dictionary)
        {
            foreach (var item in dictionary)
            {
                var auditoriumRequest = driveService.Files.Get(item.Value); //item.Value==file.Id;
                using (var streamToDownload = new MemoryStream())
                {
                    auditoriumRequest.Download(streamToDownload);
                    streamToDownload.Position = 0;
                    var br = new BinaryReader(streamToDownload);
                    var data = br.ReadBytes((int)streamToDownload.Length);

                    using (var streamToFormXML = new MemoryStream(data))
                    {
                        var xmlDocument = new XmlDocument(); 
                        xmlDocument.Load(streamToFormXML);
                        DeserializeXMLFormat(xmlDocument, item.Key);
                    }
                }
            }

            FillEventsTable();
        }

        private void UploadFileToDrive(Dictionary<string, string> dictionary)
        {
            if (ResourcesProvider.Current.Auditoriums.Count > 0) //if schedule was loaded
            {
                if (dictionary.Count == 0)
                {
                    dictionary["AuditoriumScheduleData.xml"] = "";
                    dictionary["AuditoriumApplicationsData.xml"] = "";
                    dictionary["ClassroomsData.xml"] = "";
                    dictionary["PeriodsData.xml"] = "";
                }
                foreach (var item in dictionary)
                {
                    var byteArray = SerializeXmlFormat(item.Key); //item.Key==file.Name
                    var stream = new MemoryStream(byteArray);

                    var fileMetadata = new Google.Apis.Drive.v3.Data.File();
                    fileMetadata.Name = item.Key;

                    if (item.Value != "") //item.Value==file.Id
                    {
                        var searchFiles = driveService.Files.List();
                        searchFiles.Corpus = FilesResource.ListRequest.CorpusEnum.User;
                        searchFiles.Q = "name = '" + item.Key + "'";
                        searchFiles.Fields = "files(*)";

                        var fileToMove = searchFiles.Execute().Files[0];

                        var request = driveService.Files.Update(fileMetadata, item.Value, stream, contentType);
                        request.AddParents = FolderId;
                        request.RemoveParents = fileToMove.Parents[0];
                        request.Upload();
                        IUploadProgress progress = request.Upload();
                        if (progress.Status == UploadStatus.Failed)
                        {
                            if (progress.Exception != null)
                            {
                                throw progress.Exception;
                            }
                            else
                            {
                                throw new InvalidOperationException("upload process failed");
                            }
                        }
                    }
                    else
                    {
                        fileMetadata.Parents = new List<string>() { FolderId };
                        var request = driveService.Files.Create(fileMetadata, stream, contentType);
                        request.Upload();
                    }
                }
            }
        }

        //For selection or changing existing file
        private void ShowInitialCard()
        {
            eventsTable.Children.Clear();

            var resourceDictionary = (ResourceDictionary)Application.LoadComponent(new Uri("/AuditoriumBooking;component/Styles/StylesDictionary.xaml", UriKind.Relative));

            var card = (Grid)resourceDictionary["cardStyle"];
            card.MouseLeftButtonDown += new MouseButtonEventHandler(OpenFileDialogWindow);

            eventsTable.Children.Add(card);
        }

        private void OpenFileDialogWindow(object sender, MouseButtonEventArgs e)
        {
            var filePath = SelectScheduleFile();
            if (filePath != null)
            {
                ChangeCurrentData(filePath);
            }
            else
            {
                MessageBox.Show("Для отображения данных требуется загрузить XML файл!");
            }
        }

        private void openNewFile_Click(object sender, RoutedEventArgs e)
        {
            var filePath = SelectScheduleFile();
            if (filePath != null)
            {
                ChangeCurrentData(filePath);
            }
        }

        private void ChangeCurrentData(string filePath)
        {
            var auditoriumInfos = new Dictionary<Guid, ScheduleItemInfo>();
            XMLFileParser.GetInfo(filePath, ref ResourcesProvider.Current.Classrooms, ref auditoriumInfos, ref ResourcesProvider.Current.Periods);
            ResourcesProvider.Current.Auditoriums = auditoriumInfos;

            FillEventsTable();
        }

        private string SelectScheduleFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".xml";
            openFileDialog.Filter = "Text documents (.xml)|*.xml";
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }
            return null;
        }

        //Opening AppInfoWindow(instruction)
        private void openAppInfo_Click(object sender, RoutedEventArgs e)
        {
            AppInfoForm.AppInfoForm wnd = new AppInfoForm.AppInfoForm();
            wnd.ShowInTaskbar = true;
            wnd.Show();
        }

        //private static string CreateFolder(DriveService driveService, string folderName) // если Id будет нужен, чтобы добавить туда файлы
        //{
        //    var file = new File();
        //    file.Name = folderName;
        //    file.MimeType = "application/vnd.google-apps.folder";

        //    var request = driveService.Files.List();
        //    request.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}'";
        //    var response = request.Execute();
        //    if (!response.Files.Any())
        //    {
        //        var result = driveService.Files.Create(file);
        //        result.Fields = "id";
        //        return result.Execute().Id;
        //    }
        //    else
        //    {
        //        return response.Files[0].Id;
        //    }
        //}

        //Table creation
        private void FillEventsTable()
        {
            CreatePeriodIndicators();
            GenerateWindowBody();
            MakeScheduleViewByAddingLines();
            TimesTitlesSet();
            ClassesTitlesSet();
        }

        private void MakeScheduleViewByAddingLines()
        {
            gridBookingTable.Children.Clear();
            gridBookingTable.ColumnDefinitions.Clear();
            gridBookingTable.RowDefinitions.Clear();

            for (int i = 0; i < 24 - 8; i++) //minus first 8 hours
            {
                var column = new ColumnDefinition() { Width = new GridLength(cellWidth) };
                gridBookingTable.ColumnDefinitions.Add(column);
            }

            for (int i = 0; i < ResourcesProvider.Current.Classrooms.Count; i++)
            {
                var row = new RowDefinition() { Height = new GridLength(cellHeight) };
                gridBookingTable.RowDefinitions.Add(row);
            }
        }

        private void TimesTitlesSet()
        {
            timesTitles.Children.Clear();
            timesTitles.RowDefinitions.Clear();
            timesTitles.ColumnDefinitions.Clear();

            for (int i = 0; i < 24 - 1 - 8; i++)
            {
                var column = new ColumnDefinition() { Width = new GridLength(cellWidth) };
                timesTitles.ColumnDefinitions.Add(column);
            }

            var lastColumn = new ColumnDefinition() { Width = new GridLength(cellWidth + 90) }; //последний столбец предусмотрен ещё под 90 пикселей - это размер scroll
            timesTitles.ColumnDefinitions.Add(lastColumn);

            for (int j = 1; j <= 24 - 8; j++)
            {
                var textBlock = new TextBlock() { 
                    Height = cellHeight, 
                    Width = cellWidth, 
                    Padding = new Thickness(5), 
                    FontSize = 14, 
                    HorizontalAlignment = HorizontalAlignment.Left, 
                    VerticalAlignment = VerticalAlignment.Top, 
                    TextWrapping = TextWrapping.Wrap, 
                    TextTrimming = TextTrimming.WordEllipsis};
                
                textBlock.Text = string.Format("{0}:00", j - 1 + 8);

                Grid.SetColumn(textBlock, j - 1);
                Grid.SetRow(textBlock, 0);

                timesTitles.Children.Add(textBlock);
            }
        }

        private void ClassesTitlesSet()
        {
            classesTitles.Children.Clear();
            classesTitles.RowDefinitions.Clear();
            classesTitles.ColumnDefinitions.Clear();

            for (int i = 0; i < ResourcesProvider.Current.Classrooms.Count - 1; i++)
            {
                var row = new RowDefinition() { Height = new GridLength(cellHeight) };
                classesTitles.RowDefinitions.Add(row);
            }

            var lastRow = new RowDefinition() { Height = new GridLength(cellHeight + 90) }; //последняя строка предусмотрена ещё под 90 пикселей - это размер scroll
            classesTitles.RowDefinitions.Add(lastRow);

            for (int j = 1; j <= ResourcesProvider.Current.Classrooms.Count; j++) //запись названий аудиторий в таблице аудиторий
            {
                var textBlock = new TextBlock() { 
                    Height = cellHeight, 
                    Width = cellWidth, 
                    HorizontalAlignment = HorizontalAlignment.Left, 
                    VerticalAlignment = VerticalAlignment.Top, 
                    Padding = new Thickness(5), 
                    FontSize = 14, 
                    TextWrapping = TextWrapping.Wrap, 
                    TextTrimming = TextTrimming.WordEllipsis 
                };

                if (j == 0)
                {
                    textBlock.Text = "";
                }
                else
                {
                    textBlock.Text = string.Format("{0}", ResourcesProvider.Current.Classrooms[j - 1].fullName);
                }

                Grid.SetColumn(textBlock, 0);
                Grid.SetRow(textBlock, j - 1);

                classesTitles.Children.Add(textBlock);
            }
        }

        public void GenerateWindowBody()
        {
            if (ResourcesProvider.Current.Auditoriums.Count == 0)
            {
                ShowInitialCard();
            }
            else
            {
                if (ResourcesProvider.Current.ChosenDate.AddDays(-1) < DateTime.Today.Date)
                {
                    labelArrowLeft.IsEnabled = false;
                }
                else
                {
                    labelArrowLeft.IsEnabled = true;
                }

                eventsTable.Children.Clear();

                var dayOfWeekEng = ResourcesProvider.Current.ChosenDate.DayOfWeek;
                var dayOfWeek = GetWeekDayOfChosenDay(dayOfWeekEng);

                for (int j = 0; j < ResourcesProvider.Current.Classrooms.Count; j++)
                {
                    var localApps = new Dictionary<Guid, IAuditoriumInfo>();
                    if (ResourcesProvider.Current.FilterWeek == Week.Znamenatel)
                    {
                        var appropriateApplications = ResourcesProvider.Current.Applications.Where(a =>
                        ((!a.Value.IsWeeklyEvent && a.Value.ParticularDate == ResourcesProvider.Current.ChosenDate) || (a.Value.IsWeeklyEvent && ResourcesProvider.Current.ChosenDate.Date.Subtract(a.Value.ParticularDate.Date).TotalHours % 7*24 == 0 && a.Value.ParticularDate <= ResourcesProvider.Current.ChosenDate)) && a.Value.RoomNumber == ResourcesProvider.Current.Classrooms[j].fullName && (a.Value.Week == "Знаменатель" || a.Value.Week == "Все недели") && !a.Value.CancelledDates.Contains(ResourcesProvider.Current.ChosenDate.Date)).ToDictionary(a => a.Key, a => a.Value);
                        var appropriateScheduleItems = ResourcesProvider.Current.Auditoriums.Where(a =>
                        (a.Value.Day == dayOfWeek && a.Value.RoomNumber == ResourcesProvider.Current.Classrooms[j].fullName && (a.Value.Week == "Знаменатель" || a.Value.Week == "Все недели") && !a.Value.CancelledDates.Contains(ResourcesProvider.Current.ChosenDate.Date))).ToDictionary(a => a.Key, a => a.Value);
                        localApps = appropriateScheduleItems.ToDictionary(a => a.Key, a => a.Value as IAuditoriumInfo).Union(appropriateApplications.ToDictionary(a => a.Key, a => a.Value as IAuditoriumInfo)).ToDictionary(a => a.Key, a => a.Value);
                    }
                    else
                    if (ResourcesProvider.Current.FilterWeek == Week.Chislitel)
                    {
                        var appropriateApplications = ResourcesProvider.Current.Applications.Where(a =>
                        ((!a.Value.IsWeeklyEvent && a.Value.ParticularDate == ResourcesProvider.Current.ChosenDate) || (a.Value.IsWeeklyEvent && ResourcesProvider.Current.ChosenDate.Date.Subtract(a.Value.ParticularDate.Date).TotalHours % 7*24 == 0 && a.Value.ParticularDate <= ResourcesProvider.Current.ChosenDate)) && a.Value.RoomNumber == ResourcesProvider.Current.Classrooms[j].fullName && (a.Value.Week == "Числитель" || a.Value.Week == "Все недели") && !a.Value.CancelledDates.Contains(ResourcesProvider.Current.ChosenDate.Date)).ToDictionary(a => a.Key, a => a.Value);
                        var appropriateScheduleItems = ResourcesProvider.Current.Auditoriums.Where(a =>
                        (a.Value.Day == dayOfWeek && a.Value.RoomNumber == ResourcesProvider.Current.Classrooms[j].fullName && (a.Value.Week == "Числитель" || a.Value.Week == "Все недели") && !a.Value.CancelledDates.Contains(ResourcesProvider.Current.ChosenDate.Date))).ToDictionary(a => a.Key, a => a.Value);
                        localApps = appropriateScheduleItems.ToDictionary(a => a.Key, a => a.Value as IAuditoriumInfo).Union(appropriateApplications.ToDictionary(a => a.Key, a => a.Value as IAuditoriumInfo)).ToDictionary(a => a.Key, a => a.Value);
                    }
                    else
                    if (ResourcesProvider.Current.FilterWeek == Week.Both)
                    {
                        var appropriateApplications = ResourcesProvider.Current.Applications.Where(a =>
                        ((!a.Value.IsWeeklyEvent && a.Value.ParticularDate == ResourcesProvider.Current.ChosenDate) || (a.Value.IsWeeklyEvent && ResourcesProvider.Current.ChosenDate.Date.Subtract(a.Value.ParticularDate.Date).TotalHours % 7*24 == 0 && a.Value.ParticularDate <= ResourcesProvider.Current.ChosenDate)) && a.Value.RoomNumber == ResourcesProvider.Current.Classrooms[j].fullName && !a.Value.CancelledDates.Contains(ResourcesProvider.Current.ChosenDate.Date)).ToDictionary(a => a.Key, a => a.Value);
                        var appropriateScheduleItems = ResourcesProvider.Current.Auditoriums.Where(a =>
                        (a.Value.Day == dayOfWeek && a.Value.RoomNumber == ResourcesProvider.Current.Classrooms[j].fullName && !a.Value.CancelledDates.Contains(ResourcesProvider.Current.ChosenDate.Date))).ToDictionary(a => a.Key, a => a.Value);
                        localApps = appropriateScheduleItems.ToDictionary(a => a.Key, a => a.Value as IAuditoriumInfo).Union(appropriateApplications.ToDictionary(a => a.Key, a => a.Value as IAuditoriumInfo)).ToDictionary(a => a.Key, a => a.Value);
                    }

                    if (localApps.Count > 0)
                    {
                        foreach (var todayEvent in localApps)
                        {
                            var oneHourWidth = cellWidth;

                            var concurrentEvents = localApps.Where(e => (e.Value.Start <= todayEvent.Value.Start && e.Value.End >= todayEvent.Value.End) ||
                            (e.Value.Start >= todayEvent.Value.Start && e.Value.End <= todayEvent.Value.End) ||
                            (e.Value.Start <= todayEvent.Value.Start && e.Value.End <= todayEvent.Value.End && e.Value.End > todayEvent.Value.Start) ||
                            (e.Value.Start >= todayEvent.Value.Start && e.Value.End >= todayEvent.Value.End && e.Value.Start < todayEvent.Value.End)).ToDictionary(a => a.Key, a => a.Value);

                            var width = oneHourWidth * (todayEvent.Value.End.Subtract(todayEvent.Value.Start).Hours + todayEvent.Value.End.Subtract(todayEvent.Value.Start).Minutes / 60.0);
                            var margLeft = oneHourWidth * (todayEvent.Value.Start.Hours + (todayEvent.Value.Start.Minutes / 60.0)) - 8 * oneHourWidth;

                            var height = cellHeight;
                            var margTop = j * cellHeight;

                            var index = GetIndexOfConcurrentEvents(todayEvent, concurrentEvents);

                            if (concurrentEvents.Count() > 1 || ((todayEvent.Value.Week == "Числитель" || todayEvent.Value.Week == "Знаменатель") && RadioButtonGroupWeekChoice.SelectedIndex == 2))
                            {
                                height = height / 2;
                                margTop = margTop + height * index;
                            }

                            var grid = new Grid() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };

                            Brush chosenBackground = new BrushConverter().ConvertFromString(todayEvent.Value.Background) as SolidColorBrush;
                            if (todayEvent.Value.SpecifiedColors.Count > 0)
                            {
                                foreach (var item in todayEvent.Value.SpecifiedColors) //if set specific color
                                {
                                    if (item.Key == ResourcesProvider.Current.ChosenDate)
                                    {
                                        chosenBackground = new BrushConverter().ConvertFromString(item.Value) as SolidColorBrush;
                                    }
                                }
                            }

                            var border = new Border()
                            {
                                CornerRadius = new CornerRadius(8),
                                BorderBrush = Brushes.Transparent,
                                BorderThickness = new Thickness(0.5),
                                Margin = new Thickness(margLeft, margTop, 0, 0),
                                Width = width,
                                Height = height,
                                Background = chosenBackground
                            };

                            string fullContent = "";
                            if (todayEvent.Value as ScheduleItemInfo != null)
                            {
                                fullContent = string.Format("{0}, \n «{1}», \n {2}, \n По расписанию", todayEvent.Value.TeacherName, todayEvent.Value.SubjectName, todayEvent.Value.GroupName);
                            }
                            else
                            {
                                if (((ApplicationInfo)todayEvent.Value).IsWeeklyEvent)
                                {
                                    fullContent = string.Format("{0}, \n «{1}», \n {2}, \n Каждую неделю, \n С: {3}", todayEvent.Value.TeacherName, todayEvent.Value.SubjectName, todayEvent.Value.GroupName, ((ApplicationInfo)todayEvent.Value).ParticularDate.Date.ToShortDateString() /*((ApplicationInfo)todayEvent.Value).EndDate.Date.ToShortDateString()*/);
                                }
                                else
                                {
                                    fullContent = string.Format("{0}, \n «{1}», \n {2}, \n Одиночное на : {3}", todayEvent.Value.TeacherName, todayEvent.Value.SubjectName, todayEvent.Value.GroupName, ((ApplicationInfo)todayEvent.Value).ParticularDate.Date.ToShortDateString());
                                }
                            }

                            var textBlock = new TextBlock()
                            {
                                Uid = todayEvent.Key.ToString(),
                                Width = width,
                                Height = height,
                                DataContext = todayEvent.Value,
                                Foreground = Brushes.White,
                                Padding = new Thickness(3),
                                TextWrapping = TextWrapping.Wrap,
                                TextTrimming = TextTrimming.WordEllipsis,
                                Text = string.Format(@"{0}, «{1}» ", todayEvent.Value.TeacherName, todayEvent.Value.GroupName),
                                ToolTip = new ToolTip()
                                {
                                    Content = fullContent,
                                    Style = FindResource("toolTip") as Style
                                }
                            };

                            textBlock.MouseRightButtonDown += new MouseButtonEventHandler(textBlock_MouseRightButtonDown); //subscribe on mouse events
                            textBlock.MouseEnter += new MouseEventHandler(tb_GotFocus);
                            textBlock.MouseLeave += new MouseEventHandler(tb_LeaveFocus);

                            border.Child = textBlock;
                            grid.Children.Add(border);

                            eventsTable.Children.Add(grid);
                        }
                    }
                }
            }
        }

        private void CreatePeriodIndicators()
        {
            // Create a Line  
            foreach (var period in ResourcesProvider.Current.Periods)
            {
                var margLeft = (cellWidth * (period.Start.Hours + (period.Start.Minutes / 60.0)) - 8 * cellWidth);
                var margTop = 0;

                Rectangle rec = new Rectangle()
                {
                    Width = (cellWidth * (period.End.Hours + period.End.Minutes / 60.0 - period.Start.Hours - period.Start.Minutes / 60.0)),
                    Height = cellHeight * ResourcesProvider.Current.Classrooms.Count,
                    Fill = new BrushConverter().ConvertFromString("#FFFFEBE5") as SolidColorBrush,
                    Stroke = Brushes.LightCoral,
                    StrokeThickness = 1,
                };

                periodsCanvas.Children.Add(rec);
                Canvas.SetTop(rec, margTop);
                Canvas.SetLeft(rec, margLeft);
            }
        }

        private string GetWeekDayOfChosenDay(DayOfWeek dayOfWeekEng)
        {
            string dayOfWeek;
            switch (dayOfWeekEng)
            {
                case DayOfWeek.Monday:
                    dayOfWeek = "Понедельник";
                    break;
                case DayOfWeek.Tuesday:
                    dayOfWeek = "Вторник";
                    break;
                case DayOfWeek.Wednesday:
                    dayOfWeek = "Среда";
                    break;
                case DayOfWeek.Thursday:
                    dayOfWeek = "Четверг";
                    break;
                case DayOfWeek.Friday:
                    dayOfWeek = "Пятница";
                    break;
                case DayOfWeek.Saturday:
                    dayOfWeek = "Суббота";
                    break;
                case DayOfWeek.Sunday:
                    dayOfWeek = "Воскресенье";
                    break;
                default:
                    dayOfWeek = "Каждый день";
                    break;
            }
            return dayOfWeek;
        }

        //XAML elements handlers(обработчики)

        private void textBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)((TextBlock)sender).Parent;
            var textBlock = (TextBlock)sender;
            border.BorderBrush = Brushes.Black;
            ContextMenu cm = this.FindResource("cmForTextBlocks") as ContextMenu;
            cm.PlacementTarget = textBlock;

            cm.IsOpen = true;
        }

        private void tb_GotFocus(object sender, MouseEventArgs e)
        {
            var border = (Border)((TextBlock)sender).Parent;
            var textBlock = (TextBlock)sender;
            textBlock.Cursor = Cursors.Hand;
            border.BorderBrush = Brushes.Black;
        }

        private void tb_LeaveFocus(object sender, MouseEventArgs e)
        {
            var border = (Border)((TextBlock)sender).Parent;
            var textBlock = (TextBlock)sender;
            textBlock.Cursor = Cursors.Arrow;
            border.BorderBrush = Brushes.Transparent;
        }

        private int GetIndexOfConcurrentEvents(System.Collections.Generic.KeyValuePair<Guid, IAuditoriumInfo> e, Dictionary<Guid, IAuditoriumInfo> dictionary) //search for position in dictionary
        {
            int i = 0;
            foreach (var currentEvent in dictionary)
            {
                if (e.Key == currentEvent.Key)
                    return i;
                i++;
            }
            return -1;
        }

        private void DisableMouseWheel(object sender, MouseEventArgs e)
        {
            MouseWheelEventArgs h = (MouseWheelEventArgs)e;
            h.Handled = true;
        }

        private void SchedulerScrolViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            HeaderHoursScrolViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            HeaderClassesScrolViewer.ScrollToVerticalOffset(e.VerticalOffset);
        }

        private void ShowPreviousDay()
        {
            ResourcesProvider.Current.ChosenDate = ResourcesProvider.Current.ChosenDate.AddDays(-1);
            datePicker.SelectedDate = ResourcesProvider.Current.ChosenDate;
            GenerateWindowBody();
        }

        private void ShowNextDay()
        {
            ResourcesProvider.Current.ChosenDate = ResourcesProvider.Current.ChosenDate.AddDays(+1);
            datePicker.SelectedDate = ResourcesProvider.Current.ChosenDate;
            GenerateWindowBody();
        }

        private void Label_MouseDownLeft(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                ShowPreviousDay();
            }
        }

        private void Label_MouseDownRight(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                ShowNextDay();
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsInitialized)
            {
                ResourcesProvider.Current.ChosenDate = datePicker.SelectedDate.Value.Date;
                GenerateWindowBody();
            }
        }

        private void RadioButtonGroupWeekChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsInitialized)
            {
                if (((ListBoxItem)RadioButtonGroupWeekChoice.SelectedItem).Content.ToString() == "Числитель")
                    ResourcesProvider.Current.FilterWeek = Week.Chislitel;
                else
                if (((ListBoxItem)RadioButtonGroupWeekChoice.SelectedItem).Content.ToString() == "Знаменатель")
                    ResourcesProvider.Current.FilterWeek = Week.Znamenatel;
                else
                if (((ListBoxItem)RadioButtonGroupWeekChoice.SelectedItem).Content.ToString() == "Все недели")
                    ResourcesProvider.Current.FilterWeek = Week.Both;

                GenerateWindowBody();
            }
        }

        //Deleting and creating events, changing color
        public void deleteEventBtn_Click(object sender, RoutedEventArgs e)
        {
            var textBlock = (TextBlock)((ContextMenu)((MenuItem)sender).TemplatedParent).PlacementTarget;
            var currentEvent = (IAuditoriumInfo)(textBlock.DataContext);
            if (currentEvent.IsScheduled)
            {
                System.Windows.Style style = new System.Windows.Style();
                style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.YesButtonContentProperty, "Удалить все"));
                style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.NoButtonContentProperty, "Удалить выбранное"));
                style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.CancelButtonContentProperty, "Отмена"));
                var result = Xceed.Wpf.Toolkit.MessageBox.Show(string.Format("Событие «{0}» загружено из расписания. Хотите удалить все последующие события?", textBlock.Text), "Подтверждение", MessageBoxButton.YesNoCancel, style);
                if (result == MessageBoxResult.Yes)
                {
                    ResourcesProvider.Current.Auditoriums.Remove(Guid.Parse(textBlock.Uid.ToString()));
                }
                else
                if (result == MessageBoxResult.No)
                {
                    var cancelledDates = ResourcesProvider.Current.Auditoriums[Guid.Parse(textBlock.Uid.ToString())].CancelledDates;
                    cancelledDates.Add(ResourcesProvider.Current.ChosenDate);
                    ResourcesProvider.Current.Auditoriums[Guid.Parse(textBlock.Uid.ToString())].CancelledDates = cancelledDates;
                }
            }
            else
            {
                if (((ApplicationInfo)currentEvent).IsWeeklyEvent)
                {
                    System.Windows.Style style = new System.Windows.Style();
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.YesButtonContentProperty, "Удалить все"));
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.NoButtonContentProperty, "Удалить выбранное"));
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.CancelButtonContentProperty, "Отмена"));
                    var result = Xceed.Wpf.Toolkit.MessageBox.Show(string.Format("Событие «{0}» является повторяющимся. Хотите удалить все последующие события?", textBlock.Text), "Подтверждение", MessageBoxButton.YesNoCancel, style);
                    if (result == MessageBoxResult.Yes)
                    {
                        ResourcesProvider.Current.Applications.Remove(Guid.Parse(textBlock.Uid.ToString()));
                    }
                    else
                    if (result == MessageBoxResult.No)
                    {
                        var cancelledDates = ResourcesProvider.Current.Applications[Guid.Parse(textBlock.Uid.ToString())].CancelledDates;
                        cancelledDates.Add(ResourcesProvider.Current.ChosenDate);
                        ResourcesProvider.Current.Applications[Guid.Parse(textBlock.Uid.ToString())].CancelledDates = cancelledDates;
                    }
                }
                else
                {
                    System.Windows.Style style = new System.Windows.Style();
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.OkButtonContentProperty, "Удалить"));
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.CancelButtonContentProperty, "Отмена"));
                    var result = Xceed.Wpf.Toolkit.MessageBox.Show(string.Format("Хотите удалить одиночное событие «{0}»?", textBlock.Text), "Подтверждение", MessageBoxButton.OKCancel, style);

                    if (result == MessageBoxResult.OK)
                    {
                        ResourcesProvider.Current.Applications.Remove(Guid.Parse(textBlock.Uid.ToString()));
                    }
                }
            }

            GenerateWindowBody();
        }

        public void deleteEditedEventBtn_Click(object sender, RoutedEventArgs e)
        {
            var textBlock = (TextBlock)((ContextMenu)((MenuItem)sender).TemplatedParent).PlacementTarget;
            var currentEvent = (IAuditoriumInfo)(textBlock.DataContext);
            if (currentEvent.IsScheduled)
            {
                System.Windows.Style style = new System.Windows.Style();
                style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.YesButtonContentProperty, "Удалить все"));
                style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.NoButtonContentProperty, "Удалить выбранное"));
                style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.CancelButtonContentProperty, "Не удалять"));
                var result = Xceed.Wpf.Toolkit.MessageBox.Show(string.Format("В событие из расписания «{0}» были внесены изменения. Хотите удалить все последующие события?", textBlock.Text), "Подтверждение", MessageBoxButton.YesNoCancel, style);
                if (result == MessageBoxResult.Yes)
                {
                    ResourcesProvider.Current.Auditoriums.Remove(Guid.Parse(textBlock.Uid.ToString()));
                }
                else
                if (result == MessageBoxResult.No)
                {
                    var cancelledDates = ResourcesProvider.Current.Auditoriums[Guid.Parse(textBlock.Uid.ToString())].CancelledDates;
                    cancelledDates.Add(ResourcesProvider.Current.ChosenDate);
                    ResourcesProvider.Current.Auditoriums[Guid.Parse(textBlock.Uid.ToString())].CancelledDates = cancelledDates;
                }
            }
            else
            {
                if (((ApplicationInfo)currentEvent).IsWeeklyEvent)
                {
                    System.Windows.Style style = new System.Windows.Style();
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.YesButtonContentProperty, "Удалить все"));
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.NoButtonContentProperty, "Удалить выбранное"));
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.CancelButtonContentProperty, "Не удалять"));
                    var result = Xceed.Wpf.Toolkit.MessageBox.Show(string.Format("В повторяющееся событие «{0}» были внесены изменения. Хотите удалить все последующие события?", textBlock.Text), "Подтверждение", MessageBoxButton.YesNoCancel, style);
                    if (result == MessageBoxResult.Yes)
                    {
                        ResourcesProvider.Current.Applications.Remove(Guid.Parse(textBlock.Uid.ToString()));
                    }
                    else
                    if (result == MessageBoxResult.No)
                    {
                        var cancelledDates = ResourcesProvider.Current.Applications[Guid.Parse(textBlock.Uid.ToString())].CancelledDates;
                        cancelledDates.Add(ResourcesProvider.Current.ChosenDate);
                        ResourcesProvider.Current.Applications[Guid.Parse(textBlock.Uid.ToString())].CancelledDates = cancelledDates;
                    }
                }
                else
                {
                    System.Windows.Style style = new System.Windows.Style();
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.OkButtonContentProperty, "Удалить"));
                    style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.CancelButtonContentProperty, "Не удалять"));
                    var result = Xceed.Wpf.Toolkit.MessageBox.Show(string.Format("В одиночное событие «{0}» были внесены изменения. Хотите удалить одиночное событие «{0}»?", textBlock.Text), "Подтверждение", MessageBoxButton.OKCancel, style);

                    if (result == MessageBoxResult.OK)
                    {
                        ResourcesProvider.Current.Applications.Remove(Guid.Parse(textBlock.Uid.ToString()));
                    }
                }
            }

            GenerateWindowBody();
        }

        private void editEventBtn_Click(object sender, RoutedEventArgs e)
        {
            AuditoriumForm.EditingForm wnd = new AuditoriumForm.EditingForm(sender, e);
            wnd.ShowInTaskbar = true;

            var textBlock = (TextBlock)((ContextMenu)((MenuItem)sender).TemplatedParent).PlacementTarget;
            var currentEvent = (IAuditoriumInfo)textBlock.DataContext;

            wnd.EditingEvent = currentEvent;
            wnd.EventGuid = Guid.Parse(textBlock.Uid.ToString());
            wnd.TeacherName = currentEvent.TeacherName;
            wnd.SubjectName = currentEvent.SubjectName;
            wnd.GroupName = currentEvent.GroupName;
            for (int i = 0; i < ResourcesProvider.Current.Classrooms.Count; i++)
            {
                if (ResourcesProvider.Current.Classrooms[i].fullName == currentEvent.RoomNumber)
                {
                    wnd.classroomCB.SelectedIndex = i;
                    break;
                }
            }
            wnd.StartTimeFormat = currentEvent.Start.ToString().Substring(0, 5);
            wnd.EndTimeFormat = currentEvent.End.ToString().Substring(0, 5);
            if (currentEvent.Week == "Числитель")
                wnd.RadioButtonGroupWeekChoice.SelectedIndex = 0;
            else
                if (currentEvent.Week == "Знаменатель")
                wnd.RadioButtonGroupWeekChoice.SelectedIndex = 1;
            else
                if (currentEvent.Week == "Все недели")
                wnd.RadioButtonGroupWeekChoice.SelectedIndex = 2;

            wnd.Show();
        }

        private void createNewEventBtn_Click(object sender, RoutedEventArgs e)
        {
            AuditoriumForm.AuditoriumForm wnd = new AuditoriumForm.AuditoriumForm();
            wnd.ShowInTaskbar = true;
            wnd.Show();
        }

        private void setColorToEventBtn_Click(object sender, RoutedEventArgs e)
        {
            var textBlock = (TextBlock)((ContextMenu)((Button)sender).TemplatedParent).PlacementTarget;
            var buttonBackground = ((Button)sender).Background;
            var specificColors = new List<ScheduleInfo.KeyValuePair<DateTime, string>>();
            if (((IAuditoriumInfo)textBlock.DataContext).IsScheduled)
            {
                specificColors = ResourcesProvider.Current.Auditoriums[Guid.Parse(textBlock.Uid.ToString())].SpecifiedColors;
                SetColorToEvent(buttonBackground, specificColors);
                ResourcesProvider.Current.Auditoriums[Guid.Parse(textBlock.Uid.ToString())].SpecifiedColors = specificColors;
            }
            else
            {
                specificColors = ResourcesProvider.Current.Applications[Guid.Parse(textBlock.Uid.ToString())].SpecifiedColors;
                SetColorToEvent(buttonBackground, specificColors);
                ResourcesProvider.Current.Applications[Guid.Parse(textBlock.Uid.ToString())].SpecifiedColors = specificColors;
            }

            GenerateWindowBody();
        }

        private static void SetColorToEvent(Brush buttonBackground, List<ScheduleInfo.KeyValuePair<DateTime, string>> specificColors)
        {
            if (specificColors.Count > 0)
            {
                int i = 0;
                foreach (var item in specificColors)
                {
                    if (item.Key == ResourcesProvider.Current.ChosenDate)
                    {
                        specificColors[i].Value = buttonBackground.ToString();
                        break;
                    }
                    i++;
                }
                if (i == specificColors.Count)
                {
                    specificColors.Add(new ScheduleInfo.KeyValuePair<DateTime, string>(ResourcesProvider.Current.ChosenDate, buttonBackground.ToString()));
                }
            }
            else
            {
                specificColors.Add(new ScheduleInfo.KeyValuePair<DateTime, string>(ResourcesProvider.Current.ChosenDate, buttonBackground.ToString()));
            }
        }

        //WindowClosedEvent
        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (Window wnd in App.Current.Windows) //closing all windows if the main one closed
                wnd.Close();
            UploadFileToDrive(FileIDs);
        }
    }
}
