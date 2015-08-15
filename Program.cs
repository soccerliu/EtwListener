using Samples.Eventing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtwListener
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start listening!");

            //BackgroundWorker worker = new BackgroundWorker();
            //worker = new BackgroundWorker();
            //SetLayeredLayout(_direction.ToString());
            //worker.DoWork += Run;
            //worker.ProgressChanged += Worker_ProgressChanged;
            //worker.WorkerReportsProgress = true;
            //worker.WorkerSupportsCancellation = true;
            //worker.RunWorkerAsync();

            Guid XAudio2ProviderId = new Guid("A6A00EFD-21F2-4A99-807E-9B3BF1D90285");

            using (EventTraceWatcher watcher = new EventTraceWatcher("XAudio4", XAudio2ProviderId))
            {
                watcher.Level = TraceLevel.Verbose;
                watcher.EventArrived += delegate (object caller, EventArrivedEventArgs evnt)
                {
                    if (evnt.EventException != null)
                    {
                        // Handle the exception 
                        Console.Error.WriteLine(evnt.EventException);
                        Environment.Exit(-1);
                    }

                    if (evnt.ProviderId != XAudio2ProviderId)
                    {
                        return;
                    }
                    Console.WriteLine("Event Name: " + evnt.EventName);
                    // Filter only relevant events
                    if (evnt.EventId == 16 || evnt.EventId == 47 || evnt.EventId == 48 || evnt.EventId == 64 || evnt.EventId == 50 || evnt.EventId == 51 || evnt.EventId == 52 || evnt.EventId == 53 || evnt.EventId == 54)
                    {
                        //worker.ReportProgress(evnt.EventId, evnt.Properties);
                        Console.WriteLine("Event recieved: " + evnt.EventId.ToString());
                    }
                };

                // Start listening 
                watcher.Start();

                Console.WriteLine("Listening...Press <Enter> to exit");
                Console.ReadLine();
            }
        }
    }
}
