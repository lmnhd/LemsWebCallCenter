using System;
using System.Collections ;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.FtpClient;
using System.Net.Sockets;
using System.Net.Http;
using System.Net.Http.Headers;
using EasyHttp.Http;
using StudioBContext.Entities;
using LemsDotNetHelpers;
using System.IO;

//using iTuner;
using System.Diagnostics;

namespace LemsWebCallCenter.CallCenter
{
   
    public class StudioBCallCenter
    {
        
        public class PhotoProxy 
        {
            public int id{get;set;}
            public string title {get;set;}
            public string relativePath {get;set;}
            public string url {get;set;}
            public int width {get;set;}
            public int height {get;set;}
            public string artistName {get;set;}
            public int artistContextID {get;set;}
            public DateTime date {get;set;}
        }

        //public PhotoProxy SendPhotoToServer()
        //{
        //    //http to context/database
        //    //ftp to serverArtistPhotosFolder
        //}
       
        public class FTPConnect
        {

            private string m_User = "";
            private string m_Pass = "";
            private string m_Host = "";
            private string m_ServerPath = "/mp3/test2.mp3";
            private string m_ContentPath = "";
            private  FileStream m_file;
            private  string m_filePath;

            public delegate void ftpEventHandler(string message);
            public event ftpEventHandler FTPEvent;
            public void RaiseEvent(string message)
            {
                if (FTPEvent != null)
                {
                    FTPEvent(message);
                }
            }

            public FTPConnect(string serverAddress, string userName, string pass)
        {
            m_User = userName;
            m_Pass = pass;
            m_Host = serverAddress;
        }
            public  int MoveToServer(string contentFilePath, string serverRelativePath, string host = "", string userName = "", string pass = "")
            {
                m_ContentPath = contentFilePath;
                
                if (host != "")
                {
                    m_Host = host;
                }
                if (userName != "")
                {
                    m_User = userName;
                }
                if (serverRelativePath != "")
                {
                    m_ServerPath = serverRelativePath;
                }
                if (pass != "")
                {
                    m_Pass = pass;
                }
                BeginOpenWrite(m_ServerPath);
                return 0;
            }
            public  int GetFromServer(string fileToGetRelativeServerPath, string localDestinationFilePath, string host = "", string userName = "", string pass = "")
            {
                m_filePath = localDestinationFilePath;
                if (host != "")
                {
                    m_Host = host;
                }
                if (userName != "")
                {
                    m_User = userName;
                }

                if (pass != "")
                {
                    m_Pass = pass;
                }

                BeginOpenRead(fileToGetRelativeServerPath);
                return 0;
            }

             ManualResetEvent m_reset = new ManualResetEvent(false);

             void BeginOpenWrite(string serverpath)
            {
                // The using statement here is OK _only_ because m_reset.WaitOne()
                // causes the code to block until the async process finishes, otherwise
                // the connection object would be disposed early. In practice, you
                // typically would not wrap the following code with a using statement.
                using (FtpClient conn = new FtpClient())
                {
                    m_reset.Reset();

                    conn.Host = m_Host;
                    conn.Credentials = new NetworkCredential(m_User, m_Pass);
                    conn.BeginOpenWrite(serverpath,
                        new AsyncCallback(BeginOpenWriteCallback), conn);

                    m_reset.WaitOne();
                    conn.Disconnect();
                }
            }

             void BeginOpenWriteCallback(IAsyncResult ar)
            {
                FtpClient conn = ar.AsyncState as FtpClient;
                Stream istream = null, ostream = null;
                byte[] buf = new byte[8192];
                int read = 0;

                try
                {
                    if (conn == null)
                        throw new InvalidOperationException("The FtpControlConnection object is null!");

                    ostream = conn.EndOpenWrite(ar);
                    istream = new FileStream(m_ContentPath, FileMode.Open, FileAccess.Read);

                    while ((read = istream.Read(buf, 0, buf.Length)) > 0)
                    {
                        ostream.Write(buf, 0, read);
                    }
                    RaiseEvent("Moved file sucessfully to temp location on server...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    RaiseEvent("Error..." + ex.Message + Environment.NewLine + ex.InnerException);
                }
                finally
                {
                    if (istream != null)
                        istream.Close();

                    if (ostream != null)
                        ostream.Close();

                    m_reset.Set();
                }
            }



             void BeginOpenRead(string serverpath)
            {
                // The using statement here is OK _only_ because m_reset.WaitOne()
                // causes the code to block until the async process finishes, otherwise
                // the connection object would be disposed early. In practice, you
                // typically would not wrap the following code with a using statement.
                using (FtpClient conn = new FtpClient())
                {
                    m_reset.Reset();

                    conn.Host = m_Host;
                    conn.Credentials = new NetworkCredential(m_User, m_Pass);
                    conn.BeginOpenRead(serverpath,
                        new AsyncCallback(BeginOpenReadCallback), conn);

                    m_reset.WaitOne();
                    conn.Disconnect();
                }
            }

             void BeginOpenReadCallback(IAsyncResult ar)
            {
                FtpClient conn = ar.AsyncState as FtpClient;

                try
                {
                    if (conn == null)
                        throw new InvalidOperationException("The FtpControlConnection object is null!");

                    using (Stream istream = conn.EndOpenRead(ar))
                    {
                        byte[] buf = new byte[8192];

                        try
                        {
                            DateTime start = DateTime.Now;
                            m_file = File.Create(m_filePath);
                            while (istream.Read(buf, 0, buf.Length) > 0)
                            {
                                double perc = 0;

                                if (istream.Length > 0)
                                    perc = (double)istream.Position / (double)istream.Length;

                                m_file.Write(buf, 0, buf.Length);
                                //  Console.Write("\rTransferring: {0}/{1} {2}/s {3:p}         ",
                                //                istream.Position,
                                //                istream.Length,
                                //               (istream.Position / DateTime.Now.Subtract(start).TotalSeconds),
                                //               perc);
                            }
                            //m_file.Dispose();
                        }
                        finally
                        {
                            Console.WriteLine();
                            istream.Close();

                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    throw new Exception(ex.Message);
                }
                finally
                {
                    m_reset.Set();
                }
            }


        }

        public class HTTPConnect
        {
            private static string mServer = "http://localhost/";
            private static int mPort = 60139;
            private static string lServer = "http://localhost:60139/api/ArtistServices/";
            private static string pServer = "http://untamemusic.com/untame14/api/ArtistServices/";


            public delegate void httpEventHandler(string message);
            public event httpEventHandler CallCenterEvent;
            public void RaiseEvent(string message)
            {
                if (CallCenterEvent != null)
                {
                    CallCenterEvent(message);
                }
            }
            public enum CallType
            {
                newSong,
                getSong,
                updateSong,
                updateName,
                updateProducer,
                updateHeadline,
                updateBPM,
                updateFeatures,
                updatePrice,
                updateRemix,
                delete,
                updateIsTagged

            }
            public class ServerStatMessageHolder
            {
                public enum Status
                {
                    OK,
                    Error
                }

                public string Message { get; set; }
                public Status Stat { get; set; }
 

            }
            public class SongProxy : ServerStatMessageHolder
            {
                public CallType callType { get; set; }

                public bool DEAD { get; set; }
                public int AlbumOrder { get; set; }
                public bool AlbumSaleOnly { get; set; }
                public int ContextArtistID { get; set; }
                public int ContextUserID { get; set; }
                public decimal BPM { get; set; }
                public List<string> Features { get; set; }
                public bool Deleted { get; set; }
                public bool IsTagged { get; set; }
                public double LengthInMillis { get; set; }
                public decimal Price { get; set; }
                public int PrimaryAlbumID { get; set; }
                public int PrimaryAlbumTrackOrder { get; set; }
                public string ProducedBy { get; set; }
                public string RelativeUrl { get; set; }
                public Dictionary<string, int> AlbumsIDList { get; set; }
                public bool Remix { get; set; }
                public int ContextSongID { get; set; }
                public int SessionRunnerLocalID { get; set; }
                public string BeatMaker { get; set; }
                public int PhotoID { get; set; }
                public string PhotoRelativeUrl { get; set; }
                public string HeadLine { get; set; }
                public DateTime Date { get; set; }
                public int MusicStoreID { get; set; }
                public string Title { get; set; }

                public string ArtistName { get; set; }

            }
            public class BookingProxy : ServerStatMessageHolder
            {
                public int BookingId { get; set; }

                public DateTime when { get; set; }

                public int ArtistId { get; set; }

                public string ArtistName { get; set; }

                public int scheduledDuration { get; set; }

                public int NumExtraPeople { get; set; }

                public double MinutesUsedThisBooking { get; set; }


                public double TotalMinutesUsedToday { get; set; }

                public bool TimesUP { get; set; }



                public bool Canceled { get; set; }

                public bool Rescheduled { get; set; }

                public int RescheduleBookingId { get; set; }

                public bool IsBooked { get; set; }

                public long durationDiscrepency;
                public bool StudioClosed { get; set; }
                public bool tooManyNiggaz { get; set; }
                public bool doubleBooked { get; set; }
                public bool invalid { get; set; }
                public double minutesUntil { get; set; }
                public bool NO_BOOKINGS { get; set; }
                public int minutesLeftInCurrentBooking { get; set; }


                public DateTime canceledDate { get; set; }
            }
            public async static void testSubmitSong()
            {
                // var song = new SongProxy
                // {
                //     Title = "skdjfkdfj",
                //     ContextArtistID = 1,
                //     ContextSongID = 1,
                //     ContextUserID = 3
                // };
                // // MakeHttpPost("http://localhost:60139/api/ArtistServices/SubmitSongData",SerializeSong(song));
                //// await MakeHttpPost("http://localhost:60139/", @"api/ArtistServices/SubmitSongData", song);
                // SongRequest( song,true);
            }

            private string CreateParamString(Dictionary<string, string> parms)
            {
                var result = "?";
                if (parms.Count > 0)
                {
                    return "";
                }

                var iter = 0;
                foreach (KeyValuePair<string, string> parm in parms)
                {
                    if (iter == 0)
                    {
                        result += parm.Key + "=" + parm.Value;
                    }
                    else
                    {

                        result += "&" + parm.Key + "=" + parm.Value;
                    }



                    iter++;
                }
                return result;
            }
            private SongProxy DeserializeSong(string songJson)
            {
                return JsonConvert.DeserializeObject<SongProxy>(songJson);
            }
            private string SerializeSong(SongProxy song)
            {
                var result = JsonConvert.SerializeObject(song);
                return result;
            }


            /// <summary>
            /// Will add,update,delete or get a song based on calltype set in proxy
            /// 
            /// </summary>
            /// 
            /// <param name="song"></param>
            /// <returns></returns>
            public SongProxy SongRequest(SongProxy song, bool localhost = false, string serverBaseHttpAddress = "")
            {
                var http = new EasyHttp.Http.HttpClient();
                var end = "SubmitSongData";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }
                var url = localhost ? lServer + end : pServer + end;
                //SubmitSongData
                var response = http.Post(url, song, HttpContentTypes.ApplicationJson);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var song2 = JsonConvert.DeserializeObject<SongProxy>(response.RawText);
                    if (song2 != null)
                    {

                        return song2;
                    }
                    else
                    {
                        throw new Exception("Server returned " + response.StatusCode + "\n" + response.RawText);
                    }
                }

                return song;
            }

            public BookingProxy QuickCheckSchedule(bool localhost = false, string serverBaseHttpAddress = "")
            {
                var end = "QuickCheckSchedule/";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }
                var url = localhost ? lServer + end : pServer + end;
                var http = new EasyHttp.Http.HttpClient();
                EasyHttp.Http.HttpResponse response;
                try
                {
                    response = http.Get(url);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var result = JsonConvert.DeserializeObject<BookingProxy>(response.RawText);
                        if (result != null)
                        {
                            if (result.IsBooked)
                            {
                                result.minutesLeftInCurrentBooking = (int)result.when.Add(TimeSpan.FromHours(result.scheduledDuration)).Subtract(DateTime.Now).TotalMinutes;
                            }
                            return result;
                        }
                        else
                        {
                            throw new Exception("Server returned " + response.StatusCode + "\n" + response.RawText);
                        }
                    }
                }
                catch
                {
                    return null;
                }




                return null;
            }

            public BookingProxy QuickBookStudio(string artistName, int numHours, bool localhost = false, string serverBaseHttpAddress = "")
            {
                var http = new EasyHttp.Http.HttpClient();
                var end = "bookings/QuickBookStudio";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }
                var url = localhost ? lServer + end : pServer + end;
                //SubmitSongData
                var response = http.Get(url, new { artistName = artistName, numHours = numHours });
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var prox = JsonConvert.DeserializeObject<BookingProxy>(response.RawText);
                    if (prox != null)
                    {
                        return prox;
                    }
                    else
                    {
                        throw new Exception("Server returned " + response.StatusCode + "\n" + response.RawText);
                    }
                }

                return null;
            }

            public List<BookingProxy> GetSchedule(DateTime day, bool localhost = false, string serverBaseHttpAddress = "")
            {
                var end = "lemonhead/AddArtist";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }
                var url = localhost ? lServer + end : pServer + end;

                var http = new EasyHttp.Http.HttpClient();
                var response = http.Get(url);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<List<BookingProxy>>(response.RawText);
                    if (result != null)
                    {

                        return result;
                    }
                    else
                    {
                        throw new Exception("Server returned " + response.StatusCode + "\n" + response.RawText);
                    }
                }

                return null;
            }

            public BookingProxy CancelBooking(BookingProxy bookingToCancel, bool localhost = false, string serverBaseHttpAddress = "")
            {
                var http = new EasyHttp.Http.HttpClient();
                var end = "bookings/CancelBooking";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }
                var url = localhost ? lServer + end : pServer + end;
                //SubmitSongData
                var response = http.Post(url, bookingToCancel, HttpContentTypes.ApplicationJson);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var prox = JsonConvert.DeserializeObject<BookingProxy>(response.RawText);
                    if (prox != null)
                    {
                        return prox;
                    }
                    else
                    {
                        throw new Exception("Server returned " + response.StatusCode + "\n" + response.RawText);
                    }
                }

                return null;
            }
            public class AddArtistResult : ServerStatMessageHolder
            {
              public  int artistID { get; set; }


            }
            public int UpdateArtistLSRID(string artistName, int id, bool localhost = false, string serverBaseHttpAddress = "")
            {
                var end = "lemonhead/UpdateLSRID";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }
                var url = localhost ? lServer + end : pServer + end;

                var http = new EasyHttp.Http.HttpClient();
                var response = http.Get(url, new { name = artistName, id = id });
                if (response.StatusCode == HttpStatusCode.OK)
                {

                    int result = JsonConvert.DeserializeObject<int>(response.RawText);
                   

                        return result;
                  
                }

                return -1;
            }
            public int AddArtist(string name, string pass, bool isUntame, bool localhost = false, string serverBaseHttpAddress = "", int srlid = 0)
            {
                var end = "lemonhead/AddArtist";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }
                var url = localhost ? lServer + end : pServer + end;

                var http = new EasyHttp.Http.HttpClient();
                var response = http.Get(url, new { name = name, pass = pass, isUntame = isUntame, sessionRunnerLocalID = srlid });
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    
                    AddArtistResult result = JsonConvert.DeserializeObject<AddArtistResult>( response.RawText );
                    if (result.artistID > 0)
                    {
                        
                        return result.artistID;
                    }
                    else
                    {
                        //throw new Exception("Server returned " + response.StatusCode + "\n" + response.RawText);
                    }
                }

                return -1;
            }
            /// <summary>
            /// Returns the server path or empty string
            /// </summary>
            /// <param name="artistName"></param>
            /// <param name="IsUntame"></param>
            /// <returns></returns>
            public string CreateBounceFolderIfNotExists(string artistName, bool IsUntame, bool localhost = false, string serverBaseHttpAddress = "")
            {
                var end = "lemonhead/CreateBounceFolderIfNotExists";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }
                var url = localhost ? lServer + end : pServer + end;
                var http = new EasyHttp.Http.HttpClient();
                var response = http.Get(url, new { artistName = artistName ,IsUntame = IsUntame });
                if (response.StatusCode == HttpStatusCode.OK)
                {

                    string result = JsonConvert.DeserializeObject<string>(response.RawText);


                    return result;

                }
                return "";
            }
            public int DeleteArtist(int contextArtistID, bool localhost = false, string serverBaseHttpAddress = "")
            {
                var end = "lemonhead/DeleteAllTracesOfArtist";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }
                var url = localhost ? lServer + end : pServer + end;
                var http = new EasyHttp.Http.HttpClient();
                try
                {
                    var response = http.Get(url, new { artistID = contextArtistID, credentials = 2570 });
                    if (response.StatusCode == HttpStatusCode.OK)
                    {

                        int result = JsonConvert.DeserializeObject<int>(response.RawText);


                        return result;

                    }
                }
                catch
                {
                    return -1;
                }
               
                return -100;
            }
            public string EmptyDataBase(bool localhost = false, string serverBaseHttpAddress = "")
            {
                var end = "lemonhead/EmptyDataBase";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }
                var url = localhost ? lServer + end : pServer + end;

                var http = new EasyHttp.Http.HttpClient();
                http.Request.Timeout = 1200000;
                var response = http.Get(url);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var result = response.RawText;
                    if (result != null)
                    {

                        return result;
                    }
                    else
                    {
                        throw new Exception("Server returned " + response.StatusCode + "\n" + response.RawText);
                    }
                }

                return null;
            }

            public List<StudioBCallCenter.PhotoProxy> AddPhotosToWebsite(string pathOrDirectory, string artistName, string serverFtpAddress, string userName, string pass, bool localhost = false, string serverBaseHttpAddress = "",int creepSeconds = 0)
            {
                var finalResult = new List<StudioBCallCenter.PhotoProxy>();
                var end1 = "lemonhead/GetServerTempFileOrPath";
                var end2 = "lemonhead/UpdateArtistPhotosFromLocation";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }

                var serverPath = "";
                var http = new EasyHttp.Http.HttpClient();

                if (pathOrDirectory.Contains("."))
                {
                    end2 = "AddPhotoToDB";
                    var url1 = localhost ? lServer + end1 : pServer + end1;
                    var url2 = localhost ? lServer + end2 : pServer + end2;


                    var response1 = http.Get(url1, new
                    {
                        title = "add_image",
                        filename =
                            System.IO.Path.GetFileName(pathOrDirectory)
                    });
                    if (response1.StatusCode == HttpStatusCode.OK)
                    {
                        
                        List<string> serverPaths = JsonConvert.DeserializeObject<List<string>>(response1.RawText);

                        RaiseEvent(string.Format("Aquired server path : {0}.{1} Attempting to move files via ftp to address {2}", serverPaths[0],Environment.NewLine,serverFtpAddress));

                        var ftp = new StudioBCallCenter.FTPConnect(serverFtpAddress, userName, pass);

                        if (localhost)
                        {
                            var finalDestLocal = serverPaths[0];

                            System.IO.File.Copy(pathOrDirectory, finalDestLocal);

                        }
                        else
                        {
                            int moved = ftp.MoveToServer(pathOrDirectory, serverPaths[1]);
                        }

                        var response2 = http.Get(url2, new { artistName = artistName, imageLocationRelativeToServer = serverPaths[0] });

                        if (response2.StatusCode == HttpStatusCode.OK)
                        {
                            RaiseEvent(string.Format("The server responded with : {0} {1}",Environment.NewLine,response2.RawText));
                            finalResult.Add(JsonConvert.DeserializeObject<StudioBCallCenter.PhotoProxy>(response2.RawText));

                        }
                    }
                    else
                    {
                        return null;


                    }


                }
                else
                {
                    var url1 = localhost ? lServer + end1 : pServer + end1;
                    var url2 = localhost ? lServer + end2 : pServer + end2;
                    var urlSingleLargePhoto = localhost ? lServer + "AddPhotoToDB" : pServer + "AddPhotoToDB";
                    var smallPics = new List<string>();
                    var largePics = new List<string>();
                    foreach (string pic in System.IO.Directory.GetFiles(pathOrDirectory))
                    {
                        if (!pic.ToLower().Contains(".jpg"))
                        {
                            continue;
                        }
                        if (new System.IO.FileInfo(pic).Length > 1000000)
                        {
                            largePics.Add(pic);
                        }
                        else
                        {
                            smallPics.Add(pic);
                        }
                    }
                    RaiseEvent(string.Format("Found {0} small pics, {1} large...",smallPics.Count.ToString(),largePics.Count.ToString()));
                    var response1 = http.Get(url1, new
                    {
                        title = "add_images"

                    });
                    if (response1.StatusCode == HttpStatusCode.OK)
                    {
                        RaiseEvent("Beginning upload small images now....");
                        List<string> serverPaths = JsonConvert.DeserializeObject<List<string>>(response1.RawText);
                        serverPath = serverPaths[1];

                        var ftp = new StudioBCallCenter.FTPConnect(serverFtpAddress, userName, pass);


                        foreach (string file in smallPics)
                        {

                            var finalDestremote = string.Format("{0}/{1}", serverPath, System.IO.Path.GetFileName(file));


                            var finalDestLocal = string.Format("{0}/{1}", serverPaths[0], System.IO.Path.GetFileName(file));

                            // var finalDest = string.Format("{0}/{1}", @"/httpdocs/untame14/misc/add_images635303025888708653\\/", System.IO.Path.GetFileName(file));
                            RaiseEvent(string.Format("Moving {0} to {1}",file,localhost ? finalDestLocal : finalDestremote));
                            if (localhost)
                            {

                                System.IO.File.Copy(file, finalDestLocal);

                            }
                            else
                            {
                                int moved = ftp.MoveToServer(file, finalDestremote);
                            }


                        }
                        RaiseEvent(string.Format("Calling {0} via http...",url2));
                        var response2 = http.Get(url2, new { artistName = artistName, directoryLocationRelativeToServer = serverPaths[0], deleteDirectory = false });


                        

                        if (response2.StatusCode == HttpStatusCode.OK)
                        {
                            RaiseEvent(string.Format("Server responded with : {0}{1}",Environment.NewLine,response2.RawText));
                            finalResult = (JsonConvert.DeserializeObject<List<StudioBCallCenter.PhotoProxy>>(response2.RawText));

                        }

                        var count = largePics.Count();
                        if (count > 0)
                        {
                            RaiseEvent("Uploading " + count + " Large photos...");
                        }
                        foreach (string file in largePics)
                        {
                            var filename = System.IO.Path.GetFileName(file);

                           
                            if (localhost)
                            {
                                RaiseEvent("Uploading " + file + " to local host server...");
                                var finalDestLocal = string.Format("{0}/{1}", serverPaths[0], filename);

                                System.IO.File.Copy(file, finalDestLocal);

                            }
                            else
                            {
                                RaiseEvent("Uploading " + file + " to remote server...");
                                int moved = ftp.MoveToServer(file, serverPaths[1] + filename);
                            }


                            
                            var response3 = http.Get(urlSingleLargePhoto, new
                            {
                                artistName = artistName,
                                imageLocationRelativeToServer =
                                    string.Format("{0}/{1}", serverPaths[0], System.IO.Path.GetFileName(file)),
                                    deleteDirectory = count == 1
                            });
                            bool allreadyInDB = false;
                            if (response3.StatusCode == HttpStatusCode.OK)
                            {
                                RaiseEvent(string.Format("server responded with : {0} : {1}",response3.StatusCode,Environment.NewLine + response3.RawText));

                                try
                                {
                                    var temp = JsonConvert.DeserializeObject<StudioBCallCenter.PhotoProxy>(response3.RawText);
                                    finalResult.Add(temp);
                                    if (temp.title.ToLower().Contains("out of memory"))
                                    {
                                        RaiseEvent("Info...Cancelling upload due to Out of memory execption from server.");
                                        return finalResult;
                                    }
                                    else if (temp.title.ToLower().Contains("allready in ") && temp.id == 0)
                                    {
                                        allreadyInDB = true;
                                    }
                                    
                                }
                                catch
                                {

                                }

                               
                                count--;
                                
                                if (count > 1)
                                {
                                    RaiseEvent(count.ToString() + " uploads to go");
                                    if (creepSeconds > 0 && !allreadyInDB)
                                    {
                                        string message = string.Format("Will resume in {0} second{1}...{2}...", creepSeconds, creepSeconds == 1 ? "" : "s",Environment.NewLine);
                                        RaiseEvent(message);
                                        System.Threading.Thread.Sleep(creepSeconds * 1000);
                                    }

                                }



                            }
                        }

                    }

                    
                }
                return finalResult;
            }
            public StudioBCallCenter.PhotoProxy testGrabPhotoAndAddToSystem(string artname,string testfileserverrelativepath)
            {
                var finalResult = new List<StudioBCallCenter.PhotoProxy>();
                var end1 = "AddPhotoToDB";
                
                var url1 = lServer + end1;
                
                var http = new EasyHttp.Http.HttpClient();
                var response2 = http.Get(url1, new { artistName = artname, imageLocationRelativeToServer = testfileserverrelativepath });

                if (response2.StatusCode == HttpStatusCode.OK)
                {
                    return JsonConvert.DeserializeObject<StudioBCallCenter.PhotoProxy>(response2.RawText);
                }
                return new StudioBCallCenter.PhotoProxy();
            }


            public List<StudioBContext.Entities.Artist> GetAllArtists(bool localhost = false, string serverBaseHttpAddress = "")
            {
                var end = "lemonhead/GetAllArtists";
                if (serverBaseHttpAddress != "")
                {
                    lServer = string.Format("http://{0}/api/ArtistServices/", serverBaseHttpAddress.Replace("http://", ""));
                }
                var url = localhost ? lServer + end : pServer + end;
                var http = new EasyHttp.Http.HttpClient();
                EasyHttp.Http.HttpResponse response;
                try
                {
                    response = http.Get(url);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var result = JsonConvert.DeserializeObject<List<Artist>>(response.RawText);
                        if (result != null)
                        {
                           
                            return result;
                        }
                        else
                        {
                            throw new Exception("Server returned " + response.StatusCode + "\n" + response.RawText);
                        }
                    }
                }
                catch
                {
                    return null;
                }




                return null;
            }
        }

        



            }
                



                
                
              



       
          

}
