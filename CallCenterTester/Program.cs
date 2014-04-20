using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LemsWebCallCenter.CallCenter;

namespace CallCenterTester
{
    class Program
    {
        static void Main(string[] args)
        {
            string testPath = @"K:\wamp\www\mp3\TAG_UNTAMEMUSIC.mp3";
            
            //StudioBCallCenter.MoveToServer(testPath,"/mp3/test3.mp3");
            //FileUtils.UploadFileToFTP(testPath);
          //  StudioBCallCenter.FTPConnect.GetFromServer("/mp3/TAG_UNTAMEMUSIC - Copy.mp3", @"K:\wamp\www\mp3\Outhouse\Cc\testfile2.mp3");

          //  StudioBCallCenter.HTTPConnect.testSubmitSong();

          //  StudioBCallCenter.HTTPConnect.BookingProxy proxy = StudioBCallCenter.HTTPConnect.QuickCheckSchedule(true);

           // proxy = StudioBCallCenter.HTTPConnect.QuickBookStudio("Cc", 4, true);

            //proxy = StudioBCallCenter.HTTPConnect.QuickCheckSchedule(true);

           // List<StudioBCallCenter.HTTPConnect.BookingProxy> proxyLst = StudioBCallCenter.HTTPConnect.GetSchedule(DateTime.Now, true);
           // StudioBCallCenter.HTTPConnect.BookingProxy proxy = new StudioBCallCenter.HTTPConnect.BookingProxy { ArtistId = 66 };

          //  proxy = StudioBCallCenter.HTTPConnect.CancelBooking(proxy, true);

            var http = new StudioBCallCenter.HTTPConnect ();

            //var res = http.EmptyDataBase(true );
           // List<StudioBCallCenter.PhotoProxy> photos = http.AddPhotosToWebsite(@"C:\Users\BricklyfeA\Pictures\Photoshoot 9-13-13", "PeeWee", "50.62.160.97", "UntamePlesk", "Rollpop1!",false);
            var res = http.AddArtist("PeeWee", "password", true, true, srlid: 17);

           // List<StudioBCallCenter.PhotoProxy> prox = http.AddPhotosToWebsite(@"C:\Users\BricklyfeA\Pictures\pics 10-16-10\DSC_3145.JPG", "PeeWee45", "50.62.160.97", "UntamePlesk", "Rollpop1!", false);
            
            //C:\Users\BricklyfeA\Documents\Visual Studio 2012\Projects\UntameMusic2014\UntameMusic2014\misc\test1\fmfront5.jpg

            //var testGrabAndAddToSystem = http.testGrabPhotoAndAddToSystem("PeeWee45", @"C:\Users\BricklyfeA\Documents\Visual Studio 2012\Projects\UntameMusic2014\UntameMusic2014\misc\test1\fmfront5.jpg");

            //List<StudioBCallCenter.PhotoProxy> prox2 = http.AddPhotosToWebsite(@"C:\Users\BricklyfeA\Pictures\All Web\temp", "PeeWee45", "50.62.160.97", "UntamePlesk", "Rollpop1!", false);

            StudioBCallCenter.HTTPConnect.SongProxy songProx = http.SongRequest(new StudioBCallCenter.HTTPConnect.SongProxy
            {
                ArtistName = "PeeWee",
                BeatMaker = "Nobody",
                BPM = 140,
                callType = StudioBCallCenter.HTTPConnect.CallType.newSong,
                Date = DateTime.Now,
                HeadLine = "blah blah blah",
                LengthInMillis = 60000,
                PhotoRelativeUrl = "/mp3/PeeWee45",
                SessionRunnerLocalID = 17,
                Title = "SongTitle",
                Features = new List<string>
                {
                    "Cc"
                }

            },true );
          
            
          //  var result = http.

            Console.WriteLine("");
            Console.ReadLine();

        }
    }
}
