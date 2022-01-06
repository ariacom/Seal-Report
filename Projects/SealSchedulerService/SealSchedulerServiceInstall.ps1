#run as Administrator
sc.exe create "Seal Report Scheduler Service" binpath="C:\Program Files\Seal Report\SealSchedulerService.exe"

#remove the Service
#sc.exe delete "Seal Report Scheduler Service"