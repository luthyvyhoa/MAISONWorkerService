using MAISONApp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WSJob
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerConfig options;

        public Worker(ILogger<Worker> logger, WorkerConfig options)
        {
            _logger = logger;
            this.options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                int MultiThread = options.MultiThread;
                int CmdTimeout = options.CmdTimeout;
                DataSet dsJob = Utility.ExecuteDataSet("IMEX_GetJob");
                int TotalJob = dsJob.Tables[0].Rows.Count;
                int CountJobProcessed = TotalJob > MultiThread ? MultiThread : TotalJob;
                int NumJobProcessing = CountJobProcessed;
                int i = 0; // thứ tự job xử lý
                while (NumJobProcessing != 0 && TotalJob > 0)
                {
                    _logger.LogInformation("Worker running at: {0} with number job processing {1}", DateTimeOffset.Now, NumJobProcessing);
                    int CurrentJobProcessed = 0;// số lượng job vừa mới xử lý xong
                    for (; i < CountJobProcessed; i++)
                    {
                        int job = Convert.ToInt32(dsJob.Tables[0].Rows[i]["Job"].ToString());
                        new ThreadJob(job, CmdTimeout, 0);

                        _logger.LogInformation("Worker running at: {0} with thread job {1}", DateTimeOffset.Now, job);
                    }

                    //kiểm tra có job nào xử lý xong chưa
                    while (CurrentJobProcessed == 0 && CountJobProcessed <= TotalJob)
                    {
                        Thread.Sleep(5000);
                        DataSet dsJobProcess = Utility.ExecuteDataSet("SELECT COUNT(DISTINCT Job) FROM IMEX_JobProcess WHERE Status = 'P'");
                        CurrentJobProcessed = NumJobProcessing - Convert.ToInt32(dsJobProcess.Tables[0].Rows[0][0].ToString());

                        _logger.LogInformation("Worker running at: {0} with number job current processed {1}", DateTimeOffset.Now, CurrentJobProcessed);

                        if (CurrentJobProcessed == 0) Thread.Sleep(60000);
                    }
                    NumJobProcessing -= CurrentJobProcessed; // trừ đi số lượng đã xử lý xong

                    //tính số lượng cần xử lí tiếp theo
                    int JobNextProcess = (TotalJob - CountJobProcessed) >= CurrentJobProcessed ? CurrentJobProcessed : (TotalJob - CountJobProcessed);
                    CountJobProcessed += JobNextProcess;
                    NumJobProcessing += JobNextProcess;
                    _logger.LogInformation("Worker running at: {0} with number job processed {1}", DateTimeOffset.Now, CountJobProcessed);
                }

                await Task.Delay(options.ScheduleTime, stoppingToken);
            }
        }
    }
}
