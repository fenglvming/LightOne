﻿using Common.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Business {
    public class DataUploadingService {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Run() {
            var mongodump = @"D:\software\mongodb-win32-x86_64-2.4.5\bin\mongodump.exe";
            
            foreach (var command in BuildCommands()) {
                var info = new ProcessStartInfo(mongodump, command);
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;

                var process = Process.Start(info);

                var output = process.StandardOutput.ReadToEnd();
                Logger.Debug(output);
            }
        }

        private IEnumerable<string> BuildCommands() {
            var db = "queen";
            var output = Path.Combine(Environment.CurrentDirectory, "upload");

            var hoursAgo = 1;
            var epoch = new DateTime(1970, 1, 1).ToLocalTime();
            var startTime = (long)(DateTime.Now.AddHours(-hoursAgo) - epoch).TotalMilliseconds;
            yield return string.Format("-db {0} -c products -o {1} -q \"{{UpdateTime: {{$gte: new Date({2})}}}}\"", db, output, startTime);

            yield return string.Format("-db {0} -c categories -o {1}", db, output);

            yield return string.Format("-db {0} -c ratio_rankings -o {1}", db, output);
        }
    }
}