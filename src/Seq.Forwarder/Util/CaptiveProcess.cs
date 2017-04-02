﻿// Copyright 2016-2017 Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;
using System.Threading;

namespace Seq.Forwarder.Util
{
    public class CaptiveProcess
    {
        public static int Run(
            string fullExePath,
            string args = null,
            Action<string> writeStdout = null,
            Action<string> writeStderr = null, 
            string workingDirectory = null)
        {
            if (fullExePath == null) throw new ArgumentNullException(nameof(fullExePath));

            args = args ?? "";
            writeStdout = writeStdout ?? delegate { };
            writeStderr = writeStderr ?? delegate { };

            var startInfo = new ProcessStartInfo
                                {
                                    UseShellExecute = false,
                                    RedirectStandardError = true,
                                    RedirectStandardOutput = true,
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    CreateNoWindow = true,
                                    ErrorDialog = false,
                                    FileName = fullExePath,
                                    Arguments = args
                                };
            
            if (!string.IsNullOrEmpty(workingDirectory))
                startInfo.WorkingDirectory = workingDirectory;

            using (var process = Process.Start(startInfo))
            using (var outputComplete = new ManualResetEvent(false))
            using (var errorComplete = new ManualResetEvent(false))
            {
                // ReSharper disable AccessToDisposedClosure

                process.OutputDataReceived += (o, e) =>
                {
                    if (e.Data == null)
                        outputComplete.Set(); 
                    else
                        writeStdout(e.Data);
                };
                process.BeginOutputReadLine();

                process.ErrorDataReceived += (o, e) =>
                {
                    if (e.Data == null)
                        errorComplete.Set();
                    else
                        writeStderr(e.Data);
                };
                process.BeginErrorReadLine();

                process.WaitForExit();

                outputComplete.WaitOne();
                errorComplete.WaitOne();

                return process.ExitCode;
            }
        }
    }
}
