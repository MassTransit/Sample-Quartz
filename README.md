# Sample MassTransit Quartz Scheduler

This sample contains two projects:

1. Net461 using TopShelf
2. NetCore (linux, windows, mac...), it uses [System.ServiceProcess.ServiceController](https://www.nuget.org/packages/System.ServiceProcess.ServiceController) which allows the process to be installed on windows as a service (using sc.exe)

The persistence mechanism used in this example is SQLServer, however Quartz.net supports [all of these](https://github.com/quartznet/quartznet/tree/master/database/tables).

## Building

Connect to your development SQLServer, windows users is most likely (localdb)\MSSQLLocalDb, and [run this script](create_quartz_tables.sql)

Open the .sln, and run the QuartzService project. Done, you have a MT Scheduler!